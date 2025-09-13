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

    public string? ServiceRequirement
    {
        get
        {
            var memo = Memos.FirstOrDefault(m => m.NextPhase == AcpJobPhase.Negotiation);
            if (memo?.Content == null) return null;

            try
            {
                var contentObj = JsonSerializer.Deserialize<NegotiationPayload>(memo.Content);
                return contentObj?.ServiceRequirement?.ToString();
            }
            catch
            {
                return null;
            }
        }
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

    public string? Deliverable
    {
        get
        {
            var memo = Memos.FirstOrDefault(m => m.NextPhase == AcpJobPhase.Completed);
            return memo?.Content;
        }
    }

    public async Task<IACPAgent?> GetProviderAgentAsync()
    {
        return AcpClient != null ? await AcpClient.GetAgentAsync(ProviderAddress) : null;
    }

    public async Task<IACPAgent?> GetClientAgentAsync()
    {
        return AcpClient != null ? await AcpClient.GetAgentAsync(ClientAddress) : null;
    }

    public async Task<IACPAgent?> GetEvaluatorAgentAsync()
    {
        return AcpClient != null ? await AcpClient.GetAgentAsync(EvaluatorAddress) : null;
    }

    public ACPMemo? LatestMemo => Memos.LastOrDefault();

    private ACPMemo? GetMemoById(int memoId)
    {
        return Memos.FirstOrDefault(m => m.Id == memoId);
    }

    public async Task<Dictionary<string, object>> PayAsync(double amount, string? reason = null)
    {
        var memo = Memos.FirstOrDefault(m => m.NextPhase == AcpJobPhase.Transaction);
        if (memo == null)
            throw new InvalidOperationException("No transaction memo found");

        reason ??= $"Job {Id} paid";

        return await AcpClient!.PayJobAsync(Id, memo.Id, amount, reason);
    }

    public async Task<string> RespondAsync(bool accept, GenericPayload? payload = null, string? reason = null)
    {
        if (LatestMemo == null || LatestMemo.NextPhase != AcpJobPhase.Negotiation)
            throw new InvalidOperationException("No negotiation memo found");

        reason ??= $"Job {Id} {(accept ? "accepted" : "rejected")}";

        return await AcpClient!.RespondToJobAsync(
            Id,
            LatestMemo.Id,
            accept,
            payload != null ? JsonSerializer.Serialize(payload) : null,
            reason
        );
    }

    public async Task<string> DeliverAsync(IDeliverable deliverable)
    {
        if (LatestMemo == null || LatestMemo.NextPhase != AcpJobPhase.Evaluation)
            throw new InvalidOperationException("No transaction memo found");

        return await AcpClient!.DeliverJobAsync(Id, deliverable);
    }

    public async Task<string> EvaluateAsync(bool accept, string? reason = null)
    {
        if (LatestMemo == null || LatestMemo.NextPhase != AcpJobPhase.Completed)
            throw new InvalidOperationException("No evaluation memo found");

        reason ??= $"Job {Id} delivery {(accept ? "accepted" : "rejected")}";

        return await AcpClient!.SignMemoAsync(LatestMemo.Id, accept, reason);
    }

    // Additional methods for position management would go here
    // (open_position, respond_open_position, etc.)
}
