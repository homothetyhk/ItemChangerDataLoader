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
using System.Diagnostics;

namespace ItemChangerDataLoader
{
    public class ICDLModeMenuConstructor : ModeMenuConstructor
    {
        public ICDLModeMenuConstructor(string title, string directoryName)
        {
            this.title = title;
            this.directoryName = directoryName;
        }

        public static ICDLMenu Menu { get; private set; }
        internal static bool Finished { get; private set; }
        readonly string title;
        readonly string directoryName;

        public override void OnEnterMainMenu(MenuPage modeMenu)
        {
            Menu = new(modeMenu, title, directoryName);
            foreach (var entry in ICDLMenuAPI.startOverrides)
            {
                try
                {
                    entry.ConstructionHandler(Menu.StartOptionsPage);
                }
                catch (Exception e)
                {
                    ICDLMod.Instance.LogError($"Error constructing external menu:\n{e}");
                }
            }
            Finished = true;
        }

        public override void OnExitMainMenu()
        {
            Menu = null;
        }

        public override bool TryGetModeButton(MenuPage modeMenu, out BigButton button)
        {
            if (Menu.packSelector.Items.Count > 0)
            {
                button = Menu.modeButton;
                button.Show();
                return true;
            }
            else
            {
                button = null;
                Menu.modeButton.Hide();
                return false;
            }
        }
    }

    public class ICDLMenu
    {
        public readonly BigButton modeButton;
        readonly MenuPage selectPage;
        public MultiGridItemPanel packSelector;
        readonly MenuPage startPage;
        readonly MenuLabel titleLabel;
        readonly MenuLabel descriptionLabel;
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
                    
                    if (TryExtractMissingAssemblyError(e, out string assembly))
                    {   
                        errorLabel.Text.text = Localize("Error loading ItemChanger data from ic.json.") + "\n" + assembly + Localize(" may be missing or have the wrong version.");
                    }
                    else
                    {
                        errorLabel.Text.text = Localize("Error loading ItemChanger data from ic.json.") + "\n" + Localize("See ModLog for details.");
                    }
                    errorPage.Show();

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
                        if (TryExtractMissingAssemblyError(e, out string assembly))
                        {
                            errorLabel.Text.text = Localize($"Error deserializing ctx.json for pack {pack.Name}.") + "\n" + assembly + Localize(" may be missing or have the wrong version.");
                        }
                        else
                        {
                            errorLabel.Text.text = Localize($"Error deserializing ctx.json for pack {pack.Name}.") + "\n" + Localize("See ModLog for details.");
                        }
                        errorPage.Show();

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
                        Pack = pack,
                        Settings = s,
                        CTX = ctx,
                    };

                    titleLabel.Text.text = pack.Name;
                    descriptionLabel.Text.text = pack.Description;

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

        public class StartData
        {
            public ICPack Pack { get; init; }
            public ICSettings Settings { get; init; }
            public RandoModContext? CTX { get; init; }

            /// <summary>
            /// Computes a deterministic hash from the pack json.
            /// </summary>
            /// <returns>int32 hash value</returns>
            public int Hash()
            {
                using FileStream fs = new(Path.Combine(Pack._directory, "ic.json"), FileMode.Open, FileAccess.Read);
                using SHA256Managed sha256 = new();
                byte[] bytes = sha256.ComputeHash(fs);
                int seed = 17;
                for (int i = 0; i < bytes.Length; i++) seed = 31 * seed ^ bytes[i];

                return seed;
            }

            /// <summary>
            /// Applies the ICSettings to the save. If the pack supports rando tracking, also creates rando save data and applies the CTX.
            /// </summary>
            public void ApplySettings()
            {
                if (!Pack.SupportsRandoTracking)
                {
                    ItemChangerMod.CreateSettingsProfile(Settings);
                    return;
                }
                else
                {
                    typeof(RandomizerMod.RandomizerMod)
                        .GetProperty(nameof(RandomizerMod.RandomizerMod.RS), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .SetValue(null, new RandomizerSettings
                        {
                            GenerationSettings = CTX.GenerationSettings,
                            Context = CTX,
                            ProfileID = GameManager.instance.profileID,
                            TrackerData = new() { AllowSequenceBreaks = true, logFileName = "TrackerDataDebugHistory.txt", },
                            TrackerDataWithoutSequenceBreaks = new() { AllowSequenceBreaks = false, logFileName = "TrackerDataWithoutSequenceBreaksDebugHistory.txt", }
                        });

                    ItemChangerMod.CreateSettingsProfile(Settings);

                    typeof(LogManager)
                        .GetMethod("WriteLogs", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .Invoke(null, new object[]
                        {
                            new LogArguments
                            {
                                ctx = CTX,
                                gs = CTX.GenerationSettings,
                                randomizer = null
                            }
                        });

                    LogManager.Write(tw => JsonUtil.Serialize(tw, CTX), "RawSpoiler.json");
                    RandomizerMod.RandomizerMod.RS.TrackerData.Setup(CTX.GenerationSettings, CTX);
                    RandomizerMod.RandomizerMod.RS.TrackerDataWithoutSequenceBreaks.Setup(CTX.GenerationSettings, CTX);
                    ((MenuChangerMod)typeof(MenuChangerMod).GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .GetValue(null))
                        .Settings.resumeKey = "Randomizer";
                    return;
                }
            }
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

        private bool TryExtractMissingAssemblyError(Exception e, out string missingAssembly)
        {
            try
            {
                string message = e.Message;
                const string prefix = "Error resolving type specified in JSON ";

                if (message.StartsWith(prefix))
                {
                    missingAssembly = message.Split('\'')[1].Split(',')[1].Trim();
                    return true;
                }
            }
            catch { }
            missingAssembly = null;
            return false;
        }
    }
}
