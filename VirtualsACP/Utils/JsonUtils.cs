using System.Text.Json;

namespace VirtualsAcp.Utils;

public static class JsonUtils
{
    public static T? TryParseJsonModel<T>(string content) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static T? TryValidateModel<T>(object data) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
