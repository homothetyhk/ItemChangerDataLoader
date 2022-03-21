using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ItemChangerDataLoader
{
    public class ICPack
    {
        public ICPack() { }
        
        public static ICPack FromJson(string filePath)
        {
            try
            {
                ICPack pack = JsonUtil.Deserialize<ICPack>(filePath);
                pack._directory = Path.GetDirectoryName(filePath);
                return pack;
            }
            catch (Exception e)
            {
                ICDLMod.Instance.LogError($"Error loading pack at path {filePath}:\n{e}");
                return null;
            }
        }

        public string Author;
        public string Description;
        public string Name;
        public bool SupportsRandoTracking;
        internal string _directory;
    }
}
