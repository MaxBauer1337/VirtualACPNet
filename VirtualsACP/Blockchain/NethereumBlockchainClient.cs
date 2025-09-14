using System.Numerics;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using VirtualsAcp.Abi;
using VirtualsAcp.Configs;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Models;
using Nethereum.Hex.HexTypes;

namespace VirtualsAcp.Blockchain;

public class JobCreatedEventDTO
{
    [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint256", "jobId", 1, true)]
    public BigInteger JobId { get; set; }
    
    [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "client", 2, true)]
    public string Client { get; set; } = string.Empty;
    
    [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "provider", 3, true)]
    public string Provider { get; set; } = string.Empty;
    
    [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "evaluator", 4, true)]
    public string Evaluator { get; set; } = string.Empty;
}

public class NethereumBlockchainClient : IDisposable
{
    private readonly Web3 _web3;
    private readonly Account _account;
    private readonly AcpContractConfig _config;
    private readonly Contract _contract;
    private readonly Contract _tokenContract;
    private readonly ILogger? _logger;

    public NethereumBlockchainClient(
        string privateKey,
        AcpContractConfig config,
        ILogger? logger = null)
    {
        _config = config;
        _logger = logger;
        
        // Remove 0x prefix if present
        if (privateKey.StartsWith("0x"))
            privateKey = privateKey[2..];
            
        _account = new Account(privateKey);
        _web3 = new Web3(_account, config.RpcUrl);
        
        // Initialize contracts
        _contract = _web3.Eth.GetContract(ContractAbis.AcpAbi, config.ContractAddress);
        _tokenContract = _web3.Eth.GetContract(ContractAbis.Erc20Abi, config.PaymentTokenAddress);
    }

    public string AgentAddress => _account.Address;

    private BigInteger FormatAmount(double amount)
    {
        var amountDecimal = (decimal)amount;
        var multiplier = BigInteger.Pow(10, _config.PaymentTokenDecimals);
        return (BigInteger)(amountDecimal * (decimal)multiplier);
    }

    public async Task<string> CreateJobAsync(string providerAddress, string evaluatorAddress, DateTime expiredAt)
    {
        try
        {
            var expireTimestamp = new BigInteger(((DateTimeOffset)expiredAt).ToUnixTimeSeconds());
            
            var function = _contract.GetFunction("createJob");
            
            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                providerAddress,
                evaluatorAddress,
                expireTimestamp
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for createJob: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                providerAddress,
                evaluatorAddress,
                expireTimestamp
            );

            _logger?.LogInformation("Job creation transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create job");
            throw new AcpContractError("Failed to create job", ex);
        }
    }

    public async Task<string> ApproveAllowanceAsync(double amount)
    {
        try
        {
            var formattedAmount = FormatAmount(amount);
            
            var function = _tokenContract.GetFunction("approve");
            
            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                formattedAmount
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for approve: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                _config.ContractAddress,
                formattedAmount
            );

            _logger?.LogInformation("Allowance approval transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to approve allowance");
            throw new AcpContractError("Failed to approve allowance", ex);
        }
    }

    public async Task<string> CreateMemoAsync(
        int jobId,
        string content,
        MemoType memoType,
        bool isSecured,
        AcpJobPhase nextPhase)
    {
        try
        {
            var function = _contract.GetFunction("createMemo");
            
            // Estimate gas for the transaction
            // need from else this throws in sc
            var gasEstimate = await function.EstimateGasAsync(
                _account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
                jobId,
                content,
                (int)memoType,
                isSecured,
                (int)nextPhase
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for createMemo: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                jobId,
                content,
                (int)memoType,
                isSecured,
                (int)nextPhase
            );

            _logger?.LogInformation("Memo creation transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create memo");
            throw new AcpContractError("Failed to create memo", ex);
        }
    }

    public async Task<string> CreatePayableMemoAsync(
        int jobId,
        string content,
        double amount,
        string receiverAddress,
        double feeAmount,
        FeeType feeType,
        AcpJobPhase nextPhase,
        MemoType memoType,
        DateTime expiredAt,
        string? token = null)
    {
        try
        {
            var tokenAddress = token ?? _config.PaymentTokenAddress;
            var formattedAmount = FormatAmount(amount);
            var formattedFeeAmount = FormatAmount(feeAmount);
            var expireTimestamp = new BigInteger(((DateTimeOffset)expiredAt).ToUnixTimeSeconds());
            
            var function = _contract.GetFunction("createPayableMemo");
            
            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                jobId,
                content,
                tokenAddress,
                formattedAmount,
                receiverAddress,
                formattedFeeAmount,
                (int)feeType,
                (int)memoType,
                (int)nextPhase,
                expireTimestamp
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for createPayableMemo: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                jobId,
                content,
                tokenAddress,
                formattedAmount,
                receiverAddress,
                formattedFeeAmount,
                (int)feeType,
                (int)memoType,
                (int)nextPhase,
                expireTimestamp
            );

            _logger?.LogInformation("Payable memo creation transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create payable memo");
            throw new AcpContractError("Failed to create payable memo", ex);
        }
    }

    public async Task<string> SignMemoAsync(int memoId, bool isApproved, string reason = "")
    {
        try
        {
            var function = _contract.GetFunction("signMemo");
            
            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                memoId,
                isApproved,
                reason
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for signMemo: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                memoId,
                isApproved,
                reason
            );

            _logger?.LogInformation("Memo signing transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to sign memo");
            throw new AcpContractError("Failed to sign memo", ex);
        }
    }

    public async Task<string> SetBudgetAsync(int jobId, double budget)
    {
        try
        {
            var formattedBudget = FormatAmount(budget);
            
            var function = _contract.GetFunction("setBudget");
            
            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                jobId,
                formattedBudget
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for setBudget: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                jobId,
                formattedBudget
            );

            _logger?.LogInformation("Budget setting transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set budget");
            throw new AcpContractError("Failed to set budget", ex);
        }
    }

    public async Task<string> SetBudgetWithPaymentTokenAsync(int jobId, double budget, string? paymentTokenAddress = null)
    {
        try
        {
            var formattedBudget = FormatAmount(budget);
            var tokenAddress = paymentTokenAddress ?? _config.PaymentTokenAddress;
            
            var function = _contract.GetFunction("setBudgetWithPaymentToken");
            
            // Estimate gas for the transaction
            // need to add from here, else msg_sender is wrong and this throws in the contract
            var gasEstimate = await function.EstimateGasAsync(
                _account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
                jobId,
                formattedBudget,
                tokenAddress
            );
            
            // Add 20% buffer to the gas estimate
            var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);
            
            _logger?.LogInformation("Estimated gas for setBudgetWithPaymentToken: {GasEstimate}, using gas limit: {GasLimit}", 
                gasEstimate.Value, gasLimit);
            
            var txHash = await function.SendTransactionAsync(
                _account.Address,
                gasLimit.ToHexBigInteger(),
                new BigInteger(0).ToHexBigInteger(), // gasPrice - let Nethereum handle this
                jobId,
                formattedBudget,
                tokenAddress
            );

            _logger?.LogInformation("Budget with payment token setting transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set budget with payment token");
            throw new AcpContractError("Failed to set budget with payment token", ex);
        }
    }

    public async Task<bool> ValidateTransactionAsync(string txHash)
    {
        try
        {
            var receipt = await _web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(txHash);
            return receipt.Status.Value == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to validate transaction: {TxHash}", txHash);
            throw new AcpContractError($"Failed to validate transaction: {txHash}", ex);
        }
    }

    public async Task<BigInteger> GetJobIdFromTransactionAsync(string txHash)
    {
        try
        {
            var receipt = await _web3.Eth.TransactionManager.TransactionReceiptService.PollForReceiptAsync(txHash);
            
            if (receipt.Status.Value != 1)
                throw new TransactionFailedError($"Transaction failed: {txHash}");

            // Get the transaction to access the return data
            var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
            
            if (transaction == null)
                throw new AcpContractError($"Transaction not found: {txHash}");

            // Get the job ID directly from the transaction receipt data
            var hexData = receipt.Logs[0].Data;
            // Remove 0x prefix and leading zeros, then convert to BigInteger
            var cleanHex = hexData.StartsWith("0x") ? hexData[2..] : hexData;
            cleanHex = cleanHex.TrimStart('0');
            if (string.IsNullOrEmpty(cleanHex)) cleanHex = "0";
            var jobId = BigInteger.Parse(cleanHex, System.Globalization.NumberStyles.HexNumber);
            
            _logger?.LogInformation("Job ID extracted from transaction data: {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get job ID from transaction: {TxHash}", txHash);
            throw new AcpContractError($"Failed to get job ID from transaction: {txHash}", ex);
        }
    }

    public void Dispose()
    {
        // Web3 doesn't implement IDisposable, so nothing to dispose
    }
}
