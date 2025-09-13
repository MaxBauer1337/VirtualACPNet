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

    public async Task<int> InitiateJobAsync(
        object serviceRequirement,
        string? evaluatorAddress = null,
        DateTime? expiredAt = null)
    {
        // Validate against requirement schema if present
        if (RequirementSchema != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(serviceRequirement);
                var requirementDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                // Basic validation - in a real implementation, you'd use a proper JSON schema validator
                // For now, we'll just ensure it's valid JSON
                if (requirementDict == null)
                    throw new ArgumentException($"Invalid JSON in service requirement. Required format: {JsonSerializer.Serialize(RequirementSchema, new JsonSerializerOptions { WriteIndented = true })}");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON in service requirement. Required format: {JsonSerializer.Serialize(RequirementSchema, new JsonSerializerOptions { WriteIndented = true })}", ex);
            }
        }

        var finalServiceRequirement = new Dictionary<string, object>
        {
            ["name"] = Name
        };

        if (serviceRequirement is string str)
        {
            finalServiceRequirement["message"] = str;
        }
        else
        {
            finalServiceRequirement["serviceRequirement"] = serviceRequirement;
        }

        return await AcpClient!.InitiateJobAsync(
            ProviderAddress,
            finalServiceRequirement,
            Price,
            evaluatorAddress,
            expiredAt
        );
    }
}
