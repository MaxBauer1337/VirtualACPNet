using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class PayloadModel
{
    // Base class for all payload models
    // JSON serialization will use camelCase by default
}

public class GenericPayload : PayloadModel
{
    [JsonPropertyName("type")]
    public PayloadType Type { get; set; }

    [JsonPropertyName("data")]
    public object Data { get; set; } = new();
}

public class NegotiationPayload : PayloadModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("serviceRequirement")]
    public object? ServiceRequirement { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    // Additional properties can be added here
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

public class FundResponsePayload : PayloadModel
{
    [JsonPropertyName("reportingApiEndpoint")]
    public string ReportingApiEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("walletAddress")]
    public string? WalletAddress { get; set; }
}

public class TPSLConfig : PayloadModel
{
    [JsonPropertyName("price")]
    public double? Price { get; set; }

    [JsonPropertyName("percentage")]
    public double? Percentage { get; set; }
}

public class OpenPositionPayload : PayloadModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("chain")]
    public string? Chain { get; set; }

    [JsonPropertyName("contractAddress")]
    public string? ContractAddress { get; set; }

    [JsonPropertyName("direction")]
    public PositionDirection? Direction { get; set; }

    [JsonPropertyName("tp")]
    public TPSLConfig Tp { get; set; } = new();

    [JsonPropertyName("sl")]
    public TPSLConfig Sl { get; set; } = new();
}

public class UpdateTPSLConfig : PayloadModel
{
    [JsonPropertyName("amountPercentage")]
    public double? AmountPercentage { get; set; }
}

public class UpdatePositionPayload : PayloadModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("contractAddress")]
    public string? ContractAddress { get; set; }

    [JsonPropertyName("tp")]
    public UpdateTPSLConfig? Tp { get; set; }

    [JsonPropertyName("sl")]
    public UpdateTPSLConfig? Sl { get; set; }
}

public class ClosePositionPayload : PayloadModel
{
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public class PositionFulfilledPayload : PayloadModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("contractAddress")]
    public string ContractAddress { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "TP", "SL", "CLOSE"

    [JsonPropertyName("pnl")]
    public double Pnl { get; set; }

    [JsonPropertyName("entryPrice")]
    public double EntryPrice { get; set; }

    [JsonPropertyName("exitPrice")]
    public double ExitPrice { get; set; }
}

public class UnfulfilledPositionPayload : PayloadModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("contractAddress")]
    public string ContractAddress { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "ERROR", "PARTIAL"

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class CloseJobAndWithdrawPayload : PayloadModel
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class RequestClosePositionPayload : PayloadModel
{
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }
}
