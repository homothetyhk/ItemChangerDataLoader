using MenuChanger;
using MenuChanger.MenuElements;

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
}
