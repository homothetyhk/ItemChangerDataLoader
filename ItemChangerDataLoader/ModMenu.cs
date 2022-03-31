using ItemChanger.Internal.Menu;
using System.Diagnostics;
using static RandomizerMod.Localization;

namespace ItemChangerDataLoader
{
    public static class ModMenu
    {
        public static MenuScreen GetMenuScreen(MenuScreen returnScreen)
        {
            ModMenuScreenBuilder mmsb = new(Localize("ICDL"), returnScreen);
            mmsb.AddHorizontalOption(new(
                Localize("Backup New Rando Saves"), 
                Enum.GetNames(typeof(BackupRandoType)), 
                "", 
                i => ICDLMod.GlobalSettings.BackupNewRandoSaves = (BackupRandoType)i, 
                () => (int)ICDLMod.GlobalSettings.BackupNewRandoSaves));
            mmsb.AddButton(
                Localize("Save Temporary Backups"),
                Localize("Permanently saves rando backups created with \"Manual\" setting."),
                () =>
                {
                    try
                    {
                        if (!Directory.Exists(ICDLMod.TempDirectory)) return;
                        Directory.CreateDirectory(ICDLMod.PastRandoDirectory);

                        foreach (string dir in Directory.EnumerateDirectories(ICDLMod.TempDirectory, "user*"))
                        {
                            foreach (string pack in Directory.EnumerateDirectories(dir))
                            {
                                try
                                {
                                    DirectoryInfo di = new(pack);
                                    di.MoveTo(Path.Combine(ICDLMod.PastRandoDirectory, di.Name));
                                }
                                catch (Exception e) 
                                {
                                    ICDLMod.Instance.LogError($"Error moving temporary backups to Past Randos folder:\n{e}");
                                }
                            }
                        }
                    }
                    catch (Exception e) 
                    {
                        ICDLMod.Instance.LogError($"Error accessing directory info for Temp folder:\n{e}");
                    }
                });
            mmsb.AddButton(
                Localize("Browse ICDL Files"),
                Localize("View backups and plandos in the file explorer."),
                () =>
                {
                    try
                    {
                        Directory.CreateDirectory(ICDLMod.ICDLDirectory);
                        Process.Start(ICDLMod.ICDLDirectory);
                    }
                    catch (Exception e) 
                    {
                        ICDLMod.Instance.LogError($"Error opening ICDL directory:\n{e}");
                    }
                });
            mmsb.AddButton(
                Localize("Reload Main Menu"),
                Localize("Can only be used from main menu."),
                () =>
                {
                    if (GameManager.instance.sceneName == ItemChanger.SceneNames.Menu_Title)
                    {
                        InputHandler.Instance.StopUIInput();
                        UIManager.instance.StartCoroutine(GameManager.instance.ReturnToMainMenu(GameManager.ReturnToMainMenuSaveModes.DontSave));
                    }
                });
            return mmsb.CreateMenuScreen();
        }
    }
}
