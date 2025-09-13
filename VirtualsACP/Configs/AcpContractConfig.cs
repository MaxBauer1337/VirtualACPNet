namespace VirtualsAcp.Configs;

public class AcpContractConfig
{
    public string ChainEnv { get; set; } = string.Empty;
    public string RpcUrl { get; set; } = string.Empty;
    public int ChainId { get; set; }
    public string ContractAddress { get; set; } = string.Empty;
    public string PaymentTokenAddress { get; set; } = string.Empty;
    public int PaymentTokenDecimals { get; set; }
    public string AcpApiUrl { get; set; } = string.Empty;
    public string AlchemyPolicyId { get; set; } = string.Empty;
    public string AlchemyBaseUrl { get; set; } = string.Empty;
}

public static class Configurations
{
    // Configuration for Base Sepolia
    public static readonly AcpContractConfig BaseSepoliaConfig = new()
    {
        ChainEnv = "base-sepolia",
        RpcUrl = "https://sepolia.base.org",
        ChainId = 84532,
        ContractAddress = "0x8Db6B1c839Fc8f6bd35777E194677B67b4D51928",
        PaymentTokenAddress = "0x036CbD53842c5426634e7929541eC2318f3dCF7e",
        PaymentTokenDecimals = 6,
        AlchemyBaseUrl = "https://alchemy-proxy.virtuals.io/api/proxy/wallet",
        AlchemyPolicyId = "186aaa4a-5f57-4156-83fb-e456365a8820",
        AcpApiUrl = "https://acpx.virtuals.gg/api"
    };

    // Configuration for Base Mainnet
    public static readonly AcpContractConfig BaseMainnetConfig = new()
    {
        ChainEnv = "base",
        RpcUrl = "https://mainnet.base.org",
        ChainId = 8453,
        ContractAddress = "0x6a1FE26D54ab0d3E1e3168f2e0c0cDa5cC0A0A4A",
        PaymentTokenAddress = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913",
        PaymentTokenDecimals = 6,
        AlchemyBaseUrl = "https://alchemy-proxy-prod.virtuals.io/api/proxy/wallet",
        AlchemyPolicyId = "186aaa4a-5f57-4156-83fb-e456365a8820",
        AcpApiUrl = "https://acpx.virtuals.io/api"
    };

    // Default configuration (currently set to Base Mainnet)
    public static readonly AcpContractConfig DefaultConfig = BaseMainnetConfig;
}
