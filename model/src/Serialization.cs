namespace HyuugaGame.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class Serialization
{
    public static string ToJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }
    public static T FromJson<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
    public static string ToYaml(this object obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(obj);
    }
    public static T FromYaml<T>(this string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<T>(yaml);
    }
}