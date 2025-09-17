using System.Text.Json;
using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class ACPJob
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("providerAddress")]
    public string ProviderAddress { get; set; } = string.Empty;

    [JsonPropertyName("clientAddress")]
    public string ClientAddress { get; set; } = string.Empty;

    [JsonPropertyName("evaluatorAddress")]
    public string EvaluatorAddress { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("memos")]
    public List<ACPMemo> Memos { get; set; } = new();

    [JsonPropertyName("phase")]
    public AcpJobPhase Phase { get; set; }

    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }

    [JsonIgnore]
    public VirtualsACPClient? AcpClient { get; set; }

    public override string ToString()
    {
        return $"AcpJob(\n" +
               $"  id={Id},\n" +
               $"  provider_address='{ProviderAddress}',\n" +
               $"  memos=[{string.Join(", ", Memos.Select(m => m.ToString()))}],\n" +
               $"  phase={Phase}\n" +
               $"  context={JsonSerializer.Serialize(Context)}\n" +
               $")";
    }

    

    public string? ServiceName
    {
        get
        {
            var memo = Memos.FirstOrDefault(m => m.NextPhase == AcpJobPhase.Negotiation);
            if (memo?.Content == null) return null;

            try
            {
                var contentObj = JsonSerializer.Deserialize<NegotiationPayload>(memo.Content);
                return contentObj?.Name;
            }
            catch
            {
                return memo.Content;
            }
        }
    }

    public ACPMemo? LatestMemo => Memos.LastOrDefault();

    private ACPMemo? GetMemoById(int memoId)
    {
        return Memos.FirstOrDefault(m => m.Id == memoId);
    }    
}
