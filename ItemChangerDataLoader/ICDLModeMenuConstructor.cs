using ItemChanger;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Logging;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using ICSettings = ItemChanger.Settings;
using static RandomizerMod.Localization;

namespace ItemChangerDataLoader
{
    public class ICDLModeMenuConstructor : ModeMenuConstructor
    {
        public ICDLModeMenuConstructor(string title, string directoryName)
        {
            this.title = title;
            this.directoryName = directoryName;
        }

        ICDLMenu instance;
        string title;
        string directoryName;

        public override void OnEnterMainMenu(MenuPage modeMenu)
        {
            instance = new(modeMenu, title, directoryName);
        }

        public override void OnExitMainMenu()
        {
            instance = null;
        }

        public override bool TryGetModeButton(MenuPage modeMenu, out BigButton button)
        {
            if (instance.packSelector.Items.Count > 0)
            {
                button = instance.modeButton;
                button.Show();
                return true;
            }
            else
            {
                button = null;
                instance.modeButton.Hide();
                return false;
            }
        }
    }

    public class ICDLMenu
    {
        public BigButton modeButton;

        MenuPage selectPage;
        public MultiGridItemPanel packSelector;

        MenuPage startPage;
        MenuLabel titleLabel;
        MenuLabel descriptionLabel;
        MenuLabel[] hashLabels;
        const int hashLength = 5;
        VerticalItemPanel hashPanel;
        SmallButton copyHashButton;
        BigButton startButton;

        MenuPage errorPage;
        MenuLabel errorLabel;
        SmallButton modlogButton;
        SmallButton openFolderButton;

        StartData data;

        public ICDLMenu(MenuPage modeMenu, string title, string directoryName)
        {
            selectPage = new(title + " Select Menu", modeMenu);
            modeButton = new(modeMenu, Localize(title));
            modeButton.AddHideAndShowEvent(selectPage);
            startPage = new(title + " Start Menu", selectPage);
            errorPage = new(title + " Error Menu", selectPage);
            List<ICPack> packs = new();
            if (Directory.Exists(Path.Combine(ICDLMod.ModDirectory, directoryName)))
            {
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(ICDLMod.ModDirectory, directoryName)))
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
            startPage.AddToNavigationControl(startButton);

            titleLabel = new(startPage, string.Empty);
            titleLabel.MoveTo(new(0f, 300f));
            descriptionLabel = new(startPage, string.Empty, MenuLabel.Style.Body);
            descriptionLabel.MoveTo(new(-180f, 100f));

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
            startButton.SymSetNeighbor(Neighbor.Up, hashPanel);
            startPage.backButton.SymSetNeighbor(Neighbor.Down, hashPanel);

            errorLabel = new(errorPage, string.Empty, MenuLabel.Style.Body);
            errorLabel.Text.color = Color.red;
            errorLabel.MoveTo(new(-100f, 0f));
            modlogButton = new(errorPage, Localize("Open ModLog"));
            openFolderButton = new(errorPage, Localize("Browse Files"));
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
            ICSettings s = null;
            RandoModContext ctx = null;
            Thread loader = new(Load);
            loader.Start();

            void Load()
            {
                try
                {
                    s = JsonUtil.Deserialize<ICSettings>(Path.Combine(pack._directory, "ic.json"));
                }
                catch (Exception e)
                {
                    s = null;
                    ICDLMod.Instance.LogError($"Error deserializing ic.json for pack {pack.Name}:\n{e}");
                    return;
                }
                if (pack.SupportsRandoTracking)
                {
                    try
                    {
                        ctx = JsonUtil.Deserialize<RandoModContext>(Path.Combine(pack._directory, "ctx.json"));
                    }
                    catch (Exception e)
                    {
                        s = null;
                        ICDLMod.Instance.LogError($"Error deserializing ctx.json for pack {pack.Name}:\n{e}");
                        return;
                    }
                }
                ThreadSupport.BeginInvoke(Resume);
            }

            void Resume()
            {
                if (s is null)
                {
                    errorLabel.Text.text = Localize("Error loading ItemChanger data from ic.json.\nSee ModLog for details.");
                    errorPage.Show();
                    data = null;
                    return;
                }
                else if (pack.SupportsRandoTracking && ctx is null)
                {
                    errorLabel.Text.text = Localize("Error loading Randomizer data from ctx.json.\nSee ModLog for details.\nTo continue without tracker and helper features, disable SupportsRandoTracking in pack.json.");
                    errorPage.Show();
                    data = null;
                    return;
                }
                else
                {
                    data = new()
                    {
                        pack = pack,
                        settings = s,
                        ctx = ctx,
                    };

                    titleLabel.Text.text = pack.Name;
                    descriptionLabel.Text.text = pack.Description;

                    using FileStream fs = new(Path.Combine(pack._directory, "ic.json"), FileMode.Open, FileAccess.Read);
                    using SHA256Managed sha256 = new();
                    byte[] bytes = sha256.ComputeHash(fs);
                    int seed = 17;
                    for (int i = 0; i < bytes.Length; i++) seed = 31 * seed ^ bytes[i];
                    string[] hash = RandomizerMod.Menu.Hash.GetHash(seed, hashLength);
                    for (int i = 0; i < hashLength; i++)
                    {
                        hashLabels[i + 1].Text.text = Localize(hash[i]);
                    }

                    startPage.Show();
                    startButton.Button.Select();
                }
            }
            
        }


        public void ApplySettings(ICSettings settings)
        {
            ItemChangerMod.CreateSettingsProfile(settings);
        }

        public void ApplySettings(ICSettings settings, RandoModContext ctx)
        {
            typeof(RandomizerMod.RandomizerMod)
                .GetProperty(nameof(RandomizerMod.RandomizerMod.RS), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(null, new RandomizerSettings
                {
                    GenerationSettings = ctx.GenerationSettings,
                    Context = ctx,
                    ProfileID = GameManager.instance.profileID,
                    TrackerData = new() { AllowSequenceBreaks = true, logFileName = "TrackerDataDebugHistory.txt", },
                    TrackerDataWithoutSequenceBreaks = new() { AllowSequenceBreaks = false, logFileName = "TrackerDataWithoutSequenceBreaksDebugHistory.txt", }
                });

            ItemChangerMod.CreateSettingsProfile(settings);

            typeof(LogManager)
                .GetMethod("WriteLogs", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Invoke(null, new object[]
                {
                    new LogArguments
                    {
                        ctx = ctx,
                        gs = ctx.GenerationSettings,
                        randomizer = null
                    }
                });
            LogManager.Write(tw => JsonUtil.Serialize(tw, ctx), "RawSpoiler.json");
            RandomizerMod.RandomizerMod.RS.TrackerData.Setup(ctx.GenerationSettings, ctx);
            RandomizerMod.RandomizerMod.RS.TrackerDataWithoutSequenceBreaks.Setup(ctx.GenerationSettings, ctx);
        }

        public void StartGame()
        {
            MenuChangerMod.HideAllMenuPages();
            try
            {
                if (data.pack.SupportsRandoTracking) ApplySettings(data.settings, data.ctx);
                else ApplySettings(data.settings);
            }
            catch (Exception e)
            {
                ICDLMod.Instance.LogError($"Error applying loaded data from pack {data?.pack?.Name}:\n{e}");
                errorLabel.Text.text = Localize("Error applying loaded data.\nSee ModLog for details.");
                errorPage.Show();
                return;
            }

            ICDLMod.icdlStartGame = true;
            GameManager.instance.StartNewGame();
        }

        private class StartData
        {
            public ICPack pack;
            public ICSettings settings;
            public RandoModContext ctx;
        }

    }
}
