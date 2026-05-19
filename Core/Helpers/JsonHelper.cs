using Newtonsoft.Json;

namespace EnterpriseApiAutomationFramework.Core.Helpers;

public static class JsonHelper
{
    public static T ReadJson<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json)!;
    }
}