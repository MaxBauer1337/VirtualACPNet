using System.Text.Json;
using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class ACPJobOffering
{
    [JsonPropertyName("providerAddress")]
    public string ProviderAddress { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("priceUsd")]
    public double PriceUsd { get; set; }

    [JsonPropertyName("requirementSchema")]
    public Dictionary<string, object>? RequirementSchema { get; set; }

    [JsonIgnore]
    public VirtualsACPClient? AcpClient { get; set; }

    public override string ToString()
    {
        var properties = new Dictionary<string, object>
        {
            ["providerAddress"] = ProviderAddress,
            ["name"] = Name,
            ["price"] = Price,
            ["priceUsd"] = PriceUsd,
            ["requirementSchema"] = RequirementSchema ?? new Dictionary<string, object>()
        };

        return $"ACPJobOffering({JsonSerializer.Serialize(properties)})";
    }  
}
