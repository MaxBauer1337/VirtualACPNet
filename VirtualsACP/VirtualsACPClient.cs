using System.Text.Json;
using Microsoft.Extensions.Logging;
using VirtualsAcp.Blockchain;
using VirtualsAcp.Configs;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Http;
using VirtualsAcp.WebSocket;

namespace VirtualsAcp.Models;

public class VirtualsACPClient : IDisposable
{
    private readonly NethereumBlockchainClient _blockchainClient;
    private readonly AcpApiClient _apiClient;
    private readonly ACPSocketIO? _signalRClient;
    private readonly ILogger? _logger;
    private readonly AcpContractConfig _config;
    private readonly string _agentAddress;
    private readonly int _entityId;

    public event Func<ACPJob, ACPMemo?, Task>? OnNewTask;
    public event Func<ACPJob, Task<(bool, string)>>? OnEvaluate;

    public VirtualsACPClient(
        string walletPrivateKey,
        int entityId,
        string? agentWalletAddress = null,
        AcpContractConfig? config = null,
        Func<ACPJob, ACPMemo?, Task>? onNewTask = null,
        Func<ACPJob, Task<(bool, string)>>? onEvaluate = null,
        ILogger? logger = null)
    {
        _config = config ?? Configurations.DefaultConfig;
        _entityId = entityId;
        _logger = logger;

        // Initialize blockchain client
        _blockchainClient = new NethereumBlockchainClient(walletPrivateKey, _config, logger, signerAddress: agentWalletAddress);
        _agentAddress = agentWalletAddress ?? _blockchainClient.AgentAddress;

        // Initialize API client
        _apiClient = new AcpApiClient(_config.AcpApiUrl, logger);

        // Initialize SignalR client if callbacks are provided
        if (onNewTask != null || onEvaluate != null)
        {
            _signalRClient = new ACPSocketIO(_config.AcpApiUrl, logger, _agentAddress);
            _signalRClient.OnNewTask += HandleNewTaskAsync;
            _signalRClient.OnEvaluate += HandleEvaluateAsync;
            OnNewTask = onNewTask;
            OnEvaluate = onEvaluate;
        }
    }

    public string AgentAddress => _agentAddress;
    public string SignerAddress => _blockchainClient.AgentAddress;

    public async Task StartAsync()
    {
        if (_signalRClient != null)
        {
            await _signalRClient.StartAsync(_agentAddress);
        }
    }

    public async Task StopAsync()
    {
        if (_signalRClient != null)
        {
            await _signalRClient.StopAsync();
        }
    }

    private async Task HandleNewTaskAsync(object data)
    {
        try
        {
            var jobData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
            if (jobData == null) return;

            var memos = new List<ACPMemo>();
            if (jobData.TryGetValue("memos", out var memosData) && memosData is JsonElement memosElement)
            {
                foreach (var memoElement in memosElement.EnumerateArray())
                {
                    var memo = JsonSerializer.Deserialize<ACPMemo>(memoElement.GetRawText());
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }
            }

            var memoToSignId = jobData.TryGetValue("memoToSign", out var memoToSignValue)
                ? JsonSerializer.Deserialize<int?>(memoToSignValue.ToString() ?? "null")
                : null;

            var memoToSign = memoToSignId.HasValue
                ? memos.FirstOrDefault(m => m.Id == memoToSignId.Value)
                : null;

            var context = jobData.TryGetValue("context", out var contextValue)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(contextValue.ToString() ?? "{}")
                : null;

            var job = new ACPJob
            {
                Id = JsonSerializer.Deserialize<int>(jobData["id"].ToString() ?? "0"),
                ProviderAddress = jobData["providerAddress"].ToString() ?? "",
                ClientAddress = jobData["clientAddress"].ToString() ?? "",
                EvaluatorAddress = jobData["evaluatorAddress"].ToString() ?? "",
                Memos = memos,
                Phase = Enum.Parse<AcpJobPhase>(jobData["phase"].ToString() ?? "Request"),
                Price = JsonSerializer.Deserialize<double>(jobData["price"].ToString() ?? "0"),
                Context = context,
                AcpClient = this
            };

            _logger?.LogInformation("Received new task: {Job}", job.ToString());

            if (OnNewTask != null)
            {
                await OnNewTask(job, memoToSign);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling new task");
        }
    }

    private async Task HandleEvaluateAsync(object data)
    {
        try
        {
            var jobData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
            if (jobData == null) return;

            var memos = new List<ACPMemo>();
            if (jobData.TryGetValue("memos", out var memosData) && memosData is JsonElement memosElement)
            {
                foreach (var memoElement in memosElement.EnumerateArray())
                {
                    var memo = JsonSerializer.Deserialize<ACPMemo>(memoElement.GetRawText());
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }
            }

            var context = jobData.TryGetValue("context", out var contextValue)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(contextValue.ToString() ?? "{}")
                : null;

            var job = new ACPJob
            {
                Id = JsonSerializer.Deserialize<int>(jobData["id"].ToString() ?? "0"),
                ProviderAddress = jobData["providerAddress"].ToString() ?? "",
                ClientAddress = jobData["clientAddress"].ToString() ?? "",
                EvaluatorAddress = jobData["evaluatorAddress"].ToString() ?? "",
                Memos = memos,
                Phase = Enum.Parse<AcpJobPhase>(jobData["phase"].ToString() ?? "Request"),
                Price = JsonSerializer.Deserialize<double>(jobData["price"].ToString() ?? "0"),
                Context = context,
                AcpClient = this
            };

            _logger?.LogInformation("Received evaluate: {Job}", job.ToString());

            if (OnEvaluate != null)
            {
                var (accepted, reason) = await OnEvaluate(job);
                await SignMemoAsync(job.LatestMemo?.Id ?? 0, accepted, reason);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling evaluate");
        }
    }

    public async Task<List<IACPAgent>> BrowseAgentsAsync(
        string keyword,
        string? cluster = null,
        List<AcpAgentSort>? sortBy = null,
        int? topK = null,
        AcpGraduationStatus? graduationStatus = null,
        AcpOnlineStatus? onlineStatus = null)
    {
        return await _apiClient.BrowseAgentsAsync(
            keyword,
            cluster,
            sortBy,
            topK,
            graduationStatus,
            onlineStatus,
            _agentAddress
        );
    }

    public async Task<int> InitiateJobAsync(
        string providerAddress,
        object serviceRequirement,
        double amount,
        string? evaluatorAddress = null,
        DateTime? expiredAt = null)
    {
        if (expiredAt == null)
            expiredAt = DateTime.UtcNow.AddDays(1);

        var evalAddr = evaluatorAddress ?? _agentAddress;

        if (providerAddress.Equals(_agentAddress, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("You cannot initiate a job with yourself as the provider");

        // Create job on blockchain
        var txHash = await _blockchainClient.CreateJobAsync(providerAddress, evalAddr, expiredAt.Value);

        // Wait for transaction to be mined and get job ID
        await Task.Delay(3000); // Wait 3 seconds for transaction to be mined

        var jobId = await _blockchainClient.GetJobIdFromTransactionAsync(txHash);

        // Set budget
        await _blockchainClient.SetBudgetWithPaymentTokenAsync((int)jobId, amount);
        await Task.Delay(3000);

        // Create initial memo
        var memoContent = serviceRequirement is string str
            ? str
            : JsonSerializer.Serialize(serviceRequirement);

        await _blockchainClient.CreateMemoAsync(
            (int)jobId,
            memoContent,
            MemoType.Message,
            true,
            AcpJobPhase.Negotiation
        );

        _logger?.LogInformation("Initial memo for job {JobId} created", jobId);

        // Notify API
        var payload = new Dictionary<string, object>
        {
            ["jobId"] = (int)jobId,
            ["clientAddress"] = _agentAddress,
            ["providerAddress"] = providerAddress,
            ["description"] = serviceRequirement,
            ["expiredAt"] = expiredAt.Value.ToUniversalTime().ToString("O"),
            ["evaluatorAddress"] = evaluatorAddress ?? "",
            ["price"] = amount
        };

        //await _apiClient.CreateJobAsync(payload);

        return (int)jobId;
    }

    public async Task<string> RespondToJobAsync(
        int jobId,
        int memoId,
        bool accept,
        string? content,
        string? reason = "")
    {
        try
        {
            var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "", true);

            if (!accept)
                return txHash;

            await Task.Delay(10000); // Wait 10 seconds

            _logger?.LogInformation("Responding to job {JobId} with memo {MemoId} and accept {Accept} and reason {Reason}",
                jobId, memoId, accept, reason);

            await _blockchainClient.CreateMemoAsync(
                jobId,
                content ?? $"Job {jobId} accepted. {reason ?? ""}",
                MemoType.Message,
                false,
                AcpJobPhase.Transaction,
                useSmartContractSigning: true
            );

            _logger?.LogInformation("Responded to job {JobId} with memo {MemoId} and accept {Accept} and reason {Reason}",
                jobId, memoId, accept, reason);

            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in respond_to_job_memo");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> PayJobAsync(
        int jobId,
        int memoId,
        double amount,
        string? reason = "")
    {
        await _blockchainClient.ApproveAllowanceAsync(amount);

        await _blockchainClient.SignMemoAsync(memoId, true, reason ?? "");

        reason = !string.IsNullOrEmpty(reason) ? reason : $"Job {jobId} paid.";
        _logger?.LogInformation("Paid for job {JobId} with memo {MemoId} and amount {Amount} and reason {Reason}",
            jobId, memoId, amount, reason);

        var txHash = await _blockchainClient.CreateMemoAsync(
            jobId,
            reason,
            MemoType.Message,
            false,
            AcpJobPhase.Evaluation
        );

        return new Dictionary<string, object> { ["txHash"] = txHash };
    }

    public async Task<string> RequestFundsAsync(
        int jobId,
        double amount,
        string receiverAddress,
        double feeAmount,
        FeeType feeType,
        GenericPayload reason,
        AcpJobPhase nextPhase,
        DateTime expiredAt)
    {
        var txHash = await _blockchainClient.CreatePayableMemoAsync(
            jobId,
            JsonSerializer.Serialize(reason),
            amount,
            receiverAddress,
            feeAmount,
            feeType,
            nextPhase,
            MemoType.PayableRequest,
            expiredAt
        );

        return txHash;
    }

    public async Task<string> RespondToFundsRequestAsync(
        int memoId,
        bool accept,
        double amount,
        string? reason = "")
    {
        if (!accept)
        {
            var txHash = await _blockchainClient.SignMemoAsync(memoId, false, reason ?? "");
            return txHash;
        }

        if (amount > 0)
        {
            await _blockchainClient.ApproveAllowanceAsync(amount);
        }

        var signTxHash = await _blockchainClient.SignMemoAsync(memoId, true, reason ?? "");
        return signTxHash;
    }

    public async Task<string> TransferFundsAsync(
        int jobId,
        double amount,
        string receiverAddress,
        double feeAmount,
        FeeType feeType,
        GenericPayload reason,
        AcpJobPhase nextPhase,
        DateTime expiredAt)
    {
        var totalAmount = amount + feeAmount;

        if (totalAmount > 0)
        {
            await _blockchainClient.ApproveAllowanceAsync(totalAmount);
        }

        var txHash = await _blockchainClient.CreatePayableMemoAsync(
            jobId,
            JsonSerializer.Serialize(reason),
            amount,
            receiverAddress,
            feeAmount,
            feeType,
            nextPhase,
            MemoType.PayableTransferEscrow,
            expiredAt
        );

        _logger?.LogInformation(
            "Funds transferred for job {JobId} with amount {Amount} to {ReceiverAddress} and reason {Reason}, tx_hash: {TxHash}",
            jobId, amount, receiverAddress, reason, txHash);

        return txHash;
    }

    public async Task<string> SendMessageAsync(
        int jobId,
        GenericPayload message,
        AcpJobPhase nextPhase)
    {
        var txHash = await _blockchainClient.CreateMemoAsync(
            jobId,
            JsonSerializer.Serialize(message),
            MemoType.Message,
            false,
            nextPhase
        );

        return txHash;
    }

    public async Task<string> RespondToFundsTransferAsync(
        int memoId,
        bool accept,
        string? reason = "")
    {
        var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "");
        return txHash;
    }

    public async Task<string> DeliverJobAsync(int jobId, IDeliverable deliverable)
    {
        var deliverableJson = JsonSerializer.Serialize(deliverable);
        var txHash = await _blockchainClient.CreateMemoAsync(
            jobId,
            deliverableJson,
            MemoType.ObjectUrl,
            true,
            AcpJobPhase.Completed,
            useSmartContractSigning : true
        );

        return txHash;
    }

    public async Task<string> SignMemoAsync(int memoId, bool accept, string? reason = "")
    {
        var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "");
        _logger?.LogInformation("Signed memo for memo ID {MemoId} is {Status}, tx_hash: {TxHash}",
            memoId, accept ? "accepted" : "rejected", txHash);
        return txHash;
    }

    public async Task<List<ACPJob>> GetActiveJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetActiveJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCompletedJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetCompletedJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCancelledJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetCancelledJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<ACPJob?> GetJobByIdAsync(int jobId)
    {
        return await _apiClient.GetJobByIdAsync(jobId, _agentAddress);
    }

    public async Task<ACPMemo?> GetMemoByIdAsync(int jobId, int memoId)
    {
        return await _apiClient.GetMemoByIdAsync(jobId, memoId, _agentAddress);
    }

    public async Task<IACPAgent?> GetAgentAsync(string walletAddress)
    {
        return await _apiClient.GetAgentAsync(walletAddress);
    }

    public void Dispose()
    {
        _blockchainClient?.Dispose();
        _apiClient?.Dispose();
        _signalRClient?.Dispose();
    }
}
