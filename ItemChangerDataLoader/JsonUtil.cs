using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RandomizerMod.RC;

namespace ItemChangerDataLoader
{
    internal class JsonUtil
    {
        public static RandoModContext DeserializeCTX(string filepath)
        {
            return RandomizerCore.Json.JsonUtil.DeserializeFromFile<RandoModContext>(filepath);
        }

        public static void SerializeCTX(string filepath, RandoModContext ctx)
        {
            RandomizerCore.Json.JsonUtil.SerializeToFile(filepath, ctx);
        }

        public static void SerializeCTX(TextWriter tw, RandoModContext ctx)
        {
            RandomizerCore.Json.JsonUtil.GetNonLogicSerializer().Serialize(tw, ctx);
        }

        public static T DeserializeIC<T>(string filepath)
        {
            JsonSerializer js = GetICSerializer();
            using FileStream fs = new(filepath, FileMode.Open, FileAccess.Read);
            using StreamReader sr = new(fs);
            using JsonTextReader jtr = new(sr);
            return js.Deserialize<T>(jtr);
        }

        public static void SerializeIC(string filepath, object o)
        {
            JsonSerializer js = GetICSerializer();
            using FileStream fs = new(filepath, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new(fs);
            sw.NewLine = "\r\n";
            using JsonTextWriter jtw = new(sw);
            js.Serialize(jtw, o);
        }

        public static void SerializeIC(TextWriter tw, object o)
        {
            JsonSerializer js = GetICSerializer();
            tw.NewLine = "\r\n";
            using JsonTextWriter jtw = new(tw);
            js.Serialize(jtw, o);
        }

        private static JsonSerializer GetICSerializer()
        {
            JsonSerializer js = new() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Include, };
            js.Converters.Add(new StringEnumConverter());
            foreach (JsonConverter c in Modding.JsonConverterTypes.ConverterTypes) js.Converters.Add(c);
            js.Converters.Add(new ItemChanger.TaggableObject.TagListSerializer() { RemoveNewProfileTags = true });
            js.Converters.Add(new ItemChanger.Internal.ModuleCollection.ModuleListSerializer() { RemoveNewProfileModules = true });
            return js;
        }
    }
}
