using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public interface IDeliverable
{
    string Type { get; set; }
    object Value { get; set; }
}

public class Deliverable : IDeliverable
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;
}
