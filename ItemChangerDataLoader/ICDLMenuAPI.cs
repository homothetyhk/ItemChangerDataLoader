using MenuChanger;
using MenuChanger.MenuElements;

namespace ItemChangerDataLoader
{
    public delegate void OnICDLMenuConstructionHandler(MenuPage landingPage);
    public delegate bool OverrideICDLStartHandler(ICDLMenu.StartData data, MenuPage landingPage, out BaseButton button);
    internal readonly record struct ICDLStartOverride
        (OnICDLMenuConstructionHandler ConstructionHandler, OverrideICDLStartHandler StartHandler);

    public static class ICDLMenuAPI
    {
        public static ICDLMenu Menu => ICDLModeMenuConstructor.Menu;
        internal static List<ICDLStartOverride> startOverrides = new();
        public static void AddStartGameOverride(OnICDLMenuConstructionHandler constructionHandler, OverrideICDLStartHandler startHandler)
        {
            startOverrides.Add(new(constructionHandler, startHandler));
        }
        public static void RemoveStartGameOverride(OnICDLMenuConstructionHandler constructionHandler, OverrideICDLStartHandler startHandler)
        {
            startOverrides.Remove(new(constructionHandler, startHandler));
        }
    }
}
