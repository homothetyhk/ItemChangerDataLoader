using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ItemChangerDataLoader
{
    internal class JsonUtil
    {
        public static T Deserialize<T>(string filepath)
        {
            JsonSerializer js = new() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Include, };
            js.Converters.Add(new StringEnumConverter());
            using FileStream fs = new(filepath, FileMode.Open, FileAccess.Read);
            using StreamReader sr = new(fs);
            using JsonTextReader jtr = new(sr);
            return js.Deserialize<T>(jtr);
        }

        public static void Serialize(string filepath, object o)
        {
            JsonSerializer js = new() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Include, };
            js.Converters.Add(new StringEnumConverter());
            js.Converters.Add(new ItemChanger.TaggableObject.TagListSerializer() { RemoveNewProfileTags = true });
            js.Converters.Add(new ItemChanger.Internal.ModuleCollection.ModuleListSerializer() { RemoveNewProfileModules = true });
            using FileStream fs = new(filepath, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new(fs);
            sw.NewLine = "\r\n";
            using JsonTextWriter jtw = new(sw);
            js.Serialize(jtw, o);
        }
        public static void Serialize(TextWriter tw, object o)
        {
            JsonSerializer js = new() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Include, };
            js.Converters.Add(new StringEnumConverter());
            tw.NewLine = "\r\n";
            using JsonTextWriter jtw = new(tw);
            js.Serialize(jtw, o);
        }
    }
}
