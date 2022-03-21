using Modding;
using MenuChanger;
using ItemChanger;
using RandomizerMod;
using RandomizerMod.RC;
using System.Reflection;
using ICSettings = ItemChanger.Settings;
using static RandomizerMod.Localization;

namespace ItemChangerDataLoader
{
    public class ICDLMod : Mod, IGlobalSettings<GlobalSettings>, IMenuMod
    {
        public ICDLMod() : base("ICDL Mod") { }
        public override string GetVersion() => Version;
        public static ICDLMod Instance { get; private set; }
        public static GlobalSettings GlobalSettings { get; private set; } = new();

        internal static bool icdlStartGame;

        public override void Initialize()
        {
            Instance = this;
            Events.BeforeStartNewGame += BeforeStartNewGame;
            ModeMenu.AddMode(new ICDLModeMenuConstructor("Past Randos", "Past Randos"));
            ModeMenu.AddMode(new ICDLModeMenuConstructor("Plando Plando", "Plandos"));
        }

        private void BeforeStartNewGame()
        {
            if (icdlStartGame) // don't backup a backup
            {
                icdlStartGame = false;
                return;
            }

            if (GlobalSettings.BackupNewRandoSaves && RandomizerMod.RandomizerMod.IsRandoSave)
            {
                CreateRandoBackup();
            }
        }

        private void CreateRandoBackup()
        {
            ICSettings s = ItemChanger.Internal.Ref.Settings;
            RandoModContext ctx = RandomizerMod.RandomizerMod.RS.Context;

            try
            {
                DirectoryInfo di = Directory.CreateDirectory(Path.Combine(ModDirectory, "Past Randos", ctx.GenerationSettings.Seed.ToString() + " - " + DateTime.Now.ToString("yyyy-M-dd--HH-mm-ss")));
                string dir = di.FullName;
                JsonUtil.Serialize(Path.Combine(dir, "ic.json"), s);
                JsonUtil.Serialize(Path.Combine(dir, "ctx.json"), ctx);
                JsonUtil.Serialize(Path.Combine(dir, "pack.json"), new ICPack
                {
                    Name = ctx.GenerationSettings.Seed.ToString(),
                    Author = "RandomizerMod " + RandomizerMod.RandomizerMod.Version,
                    Description = string.Empty,
                    SupportsRandoTracking = true,
                });
            }
            catch (Exception e)
            {
                LogError($"Error creating rando backup:\n{e}");
            }
        }

        void IGlobalSettings<GlobalSettings>.OnLoadGlobal(GlobalSettings s)
        {
            GlobalSettings = s;
        }

        GlobalSettings IGlobalSettings<GlobalSettings>.OnSaveGlobal()
        {
            return GlobalSettings;
        }

        bool IMenuMod.ToggleButtonInsideMenu => false;

        List<IMenuMod.MenuEntry> IMenuMod.GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            string[] bools = new[]
            {
                Localize("False"), Localize("True")
            };

            return new List<IMenuMod.MenuEntry>
            {
                new(Localize("Backup New Rando Saves"), bools, "", i => GlobalSettings.BackupNewRandoSaves = i == 1, () => GlobalSettings.BackupNewRandoSaves ? 1 : 0)
            };
        }

        public static string ModDirectory { get; }
        public static string Version { get; }

        static ICDLMod()
        {
            Assembly a = typeof(ICDLMod).Assembly;
            ModDirectory = Path.GetDirectoryName(a.Location);

            Version v = a.GetName().Version;
            Version = $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}
