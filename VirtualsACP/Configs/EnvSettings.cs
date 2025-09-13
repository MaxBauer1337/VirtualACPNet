using Microsoft.Extensions.Configuration;

namespace VirtualsAcp.Configs;

public class EnvSettings
{
    public string? WhitelistedWalletPrivateKey { get; set; }
    public string? BuyerAgentWalletAddress { get; set; }
    public string? SellerAgentWalletAddress { get; set; }
    public string? EvaluatorAgentWalletAddress { get; set; }
    public string? BuyerGameTwitterAccessToken { get; set; }
    public string? SellerGameTwitterAccessToken { get; set; }
    public string? EvaluatorGameTwitterAccessToken { get; set; }
    public int? BuyerEntityId { get; set; }
    public int? SellerEntityId { get; set; }
    public int? EvaluatorEntityId { get; set; }

    public static EnvSettings FromEnvironment()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var settings = new EnvSettings();
        configuration.Bind(settings);
        return settings;
    }

    public void ValidateWalletAddresses()
    {
        ValidateWalletAddress(BuyerAgentWalletAddress, nameof(BuyerAgentWalletAddress));
        ValidateWalletAddress(SellerAgentWalletAddress, nameof(SellerAgentWalletAddress));
        ValidateWalletAddress(EvaluatorAgentWalletAddress, nameof(EvaluatorAgentWalletAddress));
    }

    private static void ValidateWalletAddress(string? address, string propertyName)
    {
        if (address == null) return;
        
        if (!address.StartsWith("0x") || address.Length != 42)
        {
            throw new ArgumentException($"Wallet address must start with '0x' and be 42 characters long.", propertyName);
        }
    }
}
