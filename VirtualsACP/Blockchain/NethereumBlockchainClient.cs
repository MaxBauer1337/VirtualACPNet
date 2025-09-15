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
                 _account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
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
        AcpJobPhase nextPhase,
        bool useSmartContractSigning = false)
    {
        try
        {
            var function = _contract.GetFunction("createMemo");

            // Check if smart contract signing is requested and signer address is available
            if (useSmartContractSigning && !string.IsNullOrEmpty(_signerAddress))
            {
                var callData = function.GetData(jobId,
                content,
                (int)memoType,
                isSecured,
                (int)nextPhase);
                return await SignWithSmartContractAsync(_signerAddress, callData);
            }

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

    public async Task<string> SignMemoAsync(int memoId, bool isApproved, string reason = "", bool useSmartContractSigning = false)
    {
        try
        {           

            var function = _contract.GetFunction("signMemo");

            // Check if smart contract signing is requested and signer address is available
            if (useSmartContractSigning && !string.IsNullOrEmpty(_signerAddress))
            {
                var callData = function.GetData(memoId, isApproved, reason);
                return await SignWithSmartContractAsync(_signerAddress, callData);
            }

            // Estimate gas for the transaction
            var gasEstimate = await function.EstimateGasAsync(
                _account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
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

    /// <summary>
    /// Signs a message using a smart contract that implements EIP-1271 signature validation.
    /// The smart contract must be deployed using ERC6900 and the account must be whitelisted.
    /// </summary>
    /// <param name="smartContractAddress">The address of the smart contract that will sign the message</param>
    /// <param name="message">The message to be signed</param>
    /// <returns>The signature hash that can be validated using EIP-1271</returns>
    public async Task<string> SignWithSmartContractAsync(
        string smartContractAddress,
        string encodedData)
    {
        try
        {
            _logger?.LogInformation("Signing message with smart contract: {SmartContractAddress}", smartContractAddress);

            // For ERC6900, we need to call the execute function with the signature data
            // This assumes the smart contract has been deployed and configured properly
            var contract = _web3.Eth.GetContract(ContractAbis.ERC6900Abi, smartContractAddress);
            var executeFunction = contract.GetFunction("execute");

            var data = encodedData.HexToByteArray();
            var gas = await executeFunction.EstimateGasAsync(_account.Address,
                new HexBigInteger(0),
                new HexBigInteger(0),
                _contract.Address, // target contract
                    0, // value
                 data);

            var txHash = await executeFunction.SendTransactionAsync(
                _account.Address,
                gas,
                new HexBigInteger(0), 
                _contract.Address, // target contract
                    0, // value
                data
                );

            _logger?.LogInformation("Smart contract signing transaction sent: {TxHash}", txHash);
            return txHash;
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
