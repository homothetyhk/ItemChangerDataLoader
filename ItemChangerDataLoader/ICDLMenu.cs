using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.RC;
using System.Threading;
using UnityEngine;
using ICSettings = ItemChanger.Settings;
using static RandomizerMod.Localization;
using System.Diagnostics;

namespace ItemChangerDataLoader
{
    public partial class ICDLMenu
    {
        public readonly BigButton modeButton;
        readonly MenuPage selectPage;
        public MultiGridItemPanel packSelector;
        readonly MenuPage startPage;
        readonly MenuLabel titleLabel;
        readonly MenuLabel descriptionLabel;
        readonly MenuLabel warningLabel;
        readonly MenuLabel[] hashLabels;
        const int hashLength = 5;
        readonly VerticalItemPanel hashPanel;
        readonly SmallButton copyHashButton;
        readonly BigButton startButton;
        readonly BigButton proceedButton;
        readonly MenuPage errorPage;
        readonly MenuLabel errorLabel;
        readonly SmallButton modlogButton;
        readonly SmallButton openFolderButton;

        public readonly MenuPage StartOptionsPage;
        readonly MultiGridItemPanel startOptionsPanel;
        readonly BigButton redirectStartButton;

        StartData data;

        public ICDLMenu(MenuPage modeMenu, string title, string directoryName)
        {
            selectPage = new(title + " Select Menu", modeMenu);
            modeButton = new(modeMenu, Localize(title));
            modeButton.AddHideAndShowEvent(selectPage);
            startPage = new(title + " Start Menu", selectPage);
            errorPage = new(title + " Error Menu", selectPage);
            StartOptionsPage = new(title + " Start Options Menu", startPage);

            List<ICPack> packs = new();
            if (Directory.Exists(Path.Combine(ICDLMod.ICDLDirectory, directoryName)))
            {
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(ICDLMod.ICDLDirectory, directoryName)))
                {
                    string path = Path.Combine(dir, "pack.json");
                    if (File.Exists(path))
                    {
                        ICPack pack = ICPack.FromJson(path);
                        if (pack != null)
                        {
                            packs.Add(pack);
                        }
                    }
                }
            }

            packSelector = new(selectPage, 5, 3, 150f, 650f, new Vector2(0, 300), packs.Select(p => CreatePackButton(p)).ToArray());

            startButton = new(startPage, Localize("Start Game"));
            startButton.OnClick += StartGame;
            startButton.MoveTo(new(0f, -300f));

            proceedButton = new(startPage, Localize("Proceed"));
            proceedButton.AddHideAndShowEvent(StartOptionsPage);
            proceedButton.MoveTo(new(0f, -300f));

            titleLabel = new(startPage, string.Empty);
            titleLabel.MoveTo(new(0f, 300f));
            descriptionLabel = new(startPage, string.Empty, MenuLabel.Style.Body);
            descriptionLabel.MoveTo(new(-180f, 100f));
            warningLabel = new(startPage, string.Empty, MenuLabel.Style.Body);
            warningLabel.MoveTo(new(-180f, -300f));
            warningLabel.Text.color = Color.yellow;

            hashLabels = new MenuLabel[hashLength + 1];
            hashLabels[0] = new MenuLabel(startPage, Localize("Hash"));
            for (int i = 1; i <= hashLength; i++)
            {
                hashLabels[i] = new(startPage, string.Empty, MenuLabel.Style.Body);
                hashLabels[i].Text.alignment = TextAnchor.UpperCenter;
            }

            copyHashButton = new(startPage, Localize("Copy Hash"));
            copyHashButton.OnClick += () =>
            {
                GUIUtility.systemCopyBuffer = string.Join(", ", hashLabels.Skip(1).Select(l => l.Text.text.Replace("\n", "")));
            };
            hashPanel = new VerticalItemPanel(startPage, new(700f, 300f), 60f, true, hashLabels.Cast<IMenuElement>().Append(copyHashButton).ToArray());
            startPage.backButton.SymSetNeighbor(Neighbor.Down, hashPanel);

            redirectStartButton = new BigButton(StartOptionsPage, "Start Normally");
            redirectStartButton.OnClick += StartGame;

            startOptionsPanel = new MultiGridItemPanel(StartOptionsPage, 5, 3, 150f, 650f, new Vector2(0, 300), Array.Empty<IMenuElement>());

            errorLabel = new(errorPage, string.Empty, MenuLabel.Style.Body);
            errorLabel.Text.color = Color.red;
            errorLabel.MoveTo(new(-100f, 0f));
            modlogButton = new(errorPage, Localize("Open ModLog"));
            modlogButton.OnClick += () => Process.Start(Path.Combine(Application.persistentDataPath, "ModLog.txt"));
            openFolderButton = new(errorPage, Localize("Browse Files"));
            openFolderButton.OnClick += () => Process.Start(ICDLMod.ICDLDirectory);
            new VerticalItemPanel(errorPage, new(400f, 50f), 100f, true, modlogButton, openFolderButton);
        }

        public BigButton CreatePackButton(ICPack pack)
        {
            BigButton bb = new(selectPage, pack.Name, pack.Author);
            bb.OnClick += () => SelectPack(pack);
            return bb;
        }

        public void SelectPack(ICPack pack)
        {
            selectPage.Hide();
            Thread loader = new(Load);
            loader.Start();

            void Load()
            {
                data = StartData.Load(pack);
                ThreadSupport.BeginInvoke(Resume);
            }

            void Resume()
            {
                if (data.ErrorMessage is not null)
                {
                    errorLabel.Text.text = data.ErrorMessage;
                    errorPage.Show();
                    data = null;
                    return;
                }
                else
                {
                    titleLabel.Text.text = pack.Name;
                    descriptionLabel.Text.text = pack.Description;
                    warningLabel.Text.text = data.WarningMessage ?? string.Empty;

                    string[] hash = RandomizerMod.Menu.Hash.GetHash(data.Hash(), hashLength);
                    for (int i = 0; i < hashLength; i++)
                    {
                        hashLabels[i + 1].Text.text = Localize(hash[i]);
                    }

                    startPage.Show();

                    BigButton nextButton;
                    if (RebuildStartOptionsPanel())
                    {
                        startButton.Hide();
                        nextButton = proceedButton;
                    }
                    else
                    {
                        proceedButton.Hide();
                        nextButton = startButton;
                    }
                    nextButton.Show();
                    nextButton.MoveTo(new(0f, -300f));
                    startPage.backButton.SymSetNeighbor(Neighbor.Up, nextButton);
                    hashPanel.SymSetNeighbor(Neighbor.Down, nextButton);
                }
            }
        }


        public void StartGame()
        {
            MenuChangerMod.HideAllMenuPages();
            try
            {
                data.ApplySettings();
            }
            catch (Exception e)
            {
                ICDLMod.Instance.LogError($"Error applying loaded data from pack {data?.Pack?.Name}:\n{e}");
                errorLabel.Text.text = Localize("Error applying loaded data.\nSee ModLog for details.");
                errorPage.Show();
                return;
            }

            ICDLMod.LocalSettings.IsICDLSave = true;
            GameManager.instance.StartNewGame();
        }

        /// <summary>
        /// Polls each subscriber to RandoStartOverride to build the PostGenerationRedirectPage. Returns true if any subscriber returns true.
        /// <br/>If this returns true, the Proceed button will be used after rando generation. Otherwise, the Start Game button will be used.
        /// </summary>
        public bool RebuildStartOptionsPanel()
        {
            if (!ICDLModeMenuConstructor.Finished) return false;

            List<BaseButton> buttons = new();
            buttons.Add(redirectStartButton);
            foreach (var entry in ICDLMenuAPI.startOverrides)
            {
                if (entry.StartHandler(data, StartOptionsPage, out BaseButton button))
                {
                    buttons.Add(button);
                }
            }
            if (buttons.Count < 2) return false;

            startOptionsPanel.Clear();
            startOptionsPanel.AddRange(buttons);
            return true;
        }

        
    }
}
