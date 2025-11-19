using Microsoft.Extensions.Logging;
using Nethereum.ABI.EIP712;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Numerics;
using System.Security.AccessControl;
using VirtualsAcp.Abi;
using VirtualsAcp.Configs;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Models;

namespace VirtualsAcp.Blockchain;

public class NethereumBlockchainClient : IDisposable
{
    private readonly Web3 _web3;
    private readonly Account _account;
    private readonly AcpContractConfig _config;
    private readonly Contract _contract;
    private readonly Contract _tokenContract;
    private readonly ILogger? _logger;
    private readonly string? _signerAddress;

    public NethereumBlockchainClient(
        string privateKey,
        AcpContractConfig config,
        ILogger? logger = null,
        string? signerAddress = null)
    {
        _config = config;
        _logger = logger;
        _signerAddress = signerAddress;

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

    private BigInteger FormatAmount(decimal amount)
    {
        var multiplier = BigInteger.Pow(10, _config.PaymentTokenDecimals);
        return (BigInteger)(amount * (decimal)multiplier);
    }

    public async Task<string> CreateJobAsync(
        string providerAddress,
        string evaluatorAddress,
        DateTime expiredAt,
        string paymentToken,
        decimal budget,
        string metadata)
    {
        try
        {
            var expireTimestamp = new BigInteger(((DateTimeOffset)expiredAt).ToUnixTimeSeconds());
            var formattedBudget = FormatAmount(budget);

            var function = _contract.GetFunction("createJob");

            string txHash = await EstimateGasAndSend(function,
                providerAddress,
                evaluatorAddress,
                expireTimestamp,
                paymentToken,
                formattedBudget,
                metadata);

            _logger?.LogInformation("Job creation transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create job");
            throw new AcpContractError("Failed to create job", ex);
        }
    }

    private async Task<string> EstimateGasAndSend(Function function, params object[] inputs)
    {
        // check if signer
        if (!string.IsNullOrEmpty(_signerAddress))
        {
            var callData = function.GetData(inputs);
            return await SignWithSmartContractAsync(function.ContractAddress, callData);
        }
        return await EstimateGasAndSendDirect(function, inputs);

    }

    private async Task<string> EstimateGasAndSendDirect(Function function, params object[] inputs)
    {
        // Estimate gas for the transaction
        var gasEstimate = await function.EstimateGasAsync(_account.Address, new HexBigInteger(0), new HexBigInteger(0), inputs);

        // Add 20% buffer to the gas estimate
        var gasLimit = gasEstimate.Value + (gasEstimate.Value / 5);

        _logger?.LogInformation("Estimated gas for createJob: {GasEstimate}, using gas limit: {GasLimit}",
            gasEstimate.Value, gasLimit);

        var cts = new CancellationTokenSource();
        var receipt = await function.SendTransactionAndWaitForReceiptAsync(
            _account.Address,
            gasLimit.ToHexBigInteger(),
            BigInteger.Zero.ToHexBigInteger(), // gasPrice - let Nethereum handle this
            cts.Token,
            inputs
        );

        return receipt.TransactionHash;
    }


    public async Task<string> ApproveAllowanceAsync(decimal amount)
    {
        try
        {
            var formattedAmount = FormatAmount(amount);

            var function = _tokenContract.GetFunction("approve");

            string txHash = await EstimateGasAndSend(function, _config.ContractAddress, formattedAmount);

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

            string txHash = await EstimateGasAndSend(function,
                jobId,
                content,
                (int)memoType,
                isSecured,
                (int)nextPhase);

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
        decimal amount,
        string receiverAddress,
        decimal feeAmount,
        FeeType feeType,
        MemoType memoType,
        DateTime expiredAt,
        bool isSecured,
        AcpJobPhase nextPhase,
        string? token = null)
    {
        try
        {
            var tokenAddress = token ?? _config.PaymentTokenAddress;
            var formattedAmount = FormatAmount(amount);
            var formattedFeeAmount = FormatAmount(feeAmount);
            var expireTimestamp = new BigInteger(((DateTimeOffset)expiredAt).ToUnixTimeSeconds());

            var function = _contract.GetFunction("createPayableMemo");

            string txHash = await EstimateGasAndSend(function,
                jobId,
                content,
                tokenAddress,
                formattedAmount,
                receiverAddress,
                formattedFeeAmount,
                (int)feeType,
                (int)memoType,
                expireTimestamp,
                isSecured,
                (int)nextPhase);

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

            string txHash = await EstimateGasAndSend(function,
                memoId,
                isApproved,
                reason);

            _logger?.LogInformation("Memo signing transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to sign memo");
            throw new AcpContractError("Failed to sign memo", ex);
        }
    }

    public async Task<string> SetBudgetWithPaymentTokenAsync(int jobId, decimal budget, string? paymentTokenAddress = null)
    {
        try
        {
            var formattedBudget = FormatAmount(budget);
            var tokenAddress = paymentTokenAddress ?? _config.PaymentTokenAddress;

            var function = _contract.GetFunction("setBudgetWithPaymentToken");

            string txHash = await EstimateGasAndSend(function,
                jobId,
                formattedBudget,
                tokenAddress);

            _logger?.LogInformation("Budget with payment token setting transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set budget with payment token");
            throw new AcpContractError("Failed to set budget with payment token", ex);
        }
    }

    public async Task<string> CreateAccountAsync(string providerAddress, string metadata)
    {
        try
        {
            var function = _contract.GetFunction("createAccount");

            string txHash = await EstimateGasAndSend(function, providerAddress, metadata);

            _logger?.LogInformation("Account creation transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create account");
            throw new AcpContractError("Failed to create account", ex);
        }
    }

    public async Task<string> CreateJobWithAccountAsync(
        int accountId,
        string evaluatorAddress,
        decimal budget,
        string paymentToken,
        DateTime expiredAt)
    {
        try
        {
            var expireTimestamp = new BigInteger(((DateTimeOffset)expiredAt).ToUnixTimeSeconds());
            var formattedBudget = FormatAmount(budget);

            var function = _contract.GetFunction("createJobWithAccount");

            string txHash = await EstimateGasAndSend(function,
                accountId,
                evaluatorAddress,
                formattedBudget,
                paymentToken,
                expireTimestamp);

            _logger?.LogInformation("Job creation with account transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create job with account");
            throw new AcpContractError("Failed to create job with account", ex);
        }
    }

    public async Task<AccountInfo> GetAccountAsync(int accountId)
    {
        try
        {
            var function = _contract.GetFunction("getAccount");
            var result = await function.CallDeserializingToObjectAsync<AccountInfo>(accountId);

            if (result == null)
            {
                throw new AcpContractError($"Account not found: {accountId}");
            }

            _logger?.LogInformation("Account retrieved: {AccountId}", accountId);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get account: {AccountId}", accountId);
            throw new AcpContractError($"Failed to get account: {accountId}", ex);
        }
    }

    public async Task<string> UpdateAccountMetadataAsync(int accountId, string metadata)
    {
        try
        {
            var function = _contract.GetFunction("updateAccountMetadata");

            string txHash = await EstimateGasAndSend(function, accountId, metadata);

            _logger?.LogInformation("Account metadata update transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update account metadata");
            throw new AcpContractError("Failed to update account metadata", ex);
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

    /// <summary>
    /// Signs a message using a smart contract that implements EIP-1271 signature validation.
    /// The smart contract must be deployed using ERC6900 and the account must be whitelisted.
    /// </summary>
    /// <param name="smartContractAddress">The address of the smart contract that will sign the message</param>
    /// <param name="message">The message to be signed</param>
    /// <returns>The signature hash that can be validated using EIP-1271</returns>
    public async Task<string> SignWithSmartContractAsync(string targetContract, string encodedData)
    {
        try
        {
            _logger?.LogInformation("Signing message with smart contract: {SmartContractAddress}", _signerAddress);

            // For ERC6900, we need to call the execute function with the signature data
            // This assumes the smart contract has been deployed and configured properly
            var contract = _web3.Eth.GetContract(ContractAbis.ERC6900Abi, _signerAddress);
            var executeFunction = contract.GetFunction("execute");

            var data = encodedData.HexToByteArray();

            var inParams = new object[] { targetContract, 0, data };
            var gas = await executeFunction.EstimateGasAsync(_account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
                inParams);

            var cts = new CancellationTokenSource();
            var txHash = await executeFunction.SendTransactionAndWaitForReceiptAsync(
                _account.Address,
                gas,
                new HexBigInteger(0),
                cts.Token,
                inParams
                );

            _logger?.LogInformation("Smart contract signing transaction sent: {TxHash}", txHash);
            return txHash.TransactionHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to sign with smart contract");
            throw new AcpContractError("Failed to sign with smart contract", ex);
        }
    }

    /// <summary>
    /// Validates a signature using EIP-1271 standard for smart contract signature verification.
    /// </summary>
    /// <param name="smartContractAddress">The address of the smart contract that signed the message</param>
    /// <param name="messageHash">The hash of the message that was signed</param>
    /// <param name="signature">The signature to validate</param>
    /// <returns>True if the signature is valid according to EIP-1271, false otherwise</returns>
    public async Task<bool> ValidateSmartContractSignatureAsync(
        string smartContractAddress,
        string messageHash,
        string signature)
    {
        try
        {
            var contract = _web3.Eth.GetContract(ContractAbis.Eip1271Abi, smartContractAddress);
            var isValidSignatureFunction = contract.GetFunction("isValidSignature");

            // Call the EIP-1271 isValidSignature function
            var result = await isValidSignatureFunction.CallAsync<byte[]>(
                messageHash.HexToByteArray(),
                signature.HexToByteArray()
            );

            // EIP-1271 returns 0x1626ba7e for valid signatures
            var validSignatureMagicValue = "0x1626ba7e";
            var isValid = result.ToHex(true).ToLower() == validSignatureMagicValue.ToLower();

            _logger?.LogInformation("Smart contract signature validation result: {IsValid} for contract: {ContractAddress}",
                isValid, smartContractAddress);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to validate smart contract signature");
            throw new AcpContractError("Failed to validate smart contract signature", ex);
        }
    }


    public void Dispose()
    {
        // Web3 doesn't implement IDisposable, so nothing to dispose
    }
}
