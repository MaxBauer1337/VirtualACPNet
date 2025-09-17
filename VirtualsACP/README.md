# VirtualsACP C# SDK

A C# SDK for the Virtuals Agent Contract Protocol (ACP), converted from the original Python/NodeJs implementation.

## Features

- **Blockchain Integration**: Uses Nethereum for Ethereum blockchain interactions
- **SocketIO Support**: Real-time communication using SocketIO
- **REST API Client**: HTTP client for ACP API interactions
- **Type Safety**: Strongly typed models and enums
- **Async/Await**: Full async support throughout the SDK
- **Logging**: Built-in logging support with Microsoft.Extensions.Logging


## Usage Example

```csharp
using VirtualsAcp.Models;
using VirtualsAcp.Configs;
using Microsoft.Extensions.Logging;

// Create logger (optional)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

// Initialize client
var client = new VirtualsACPClient(
    walletPrivateKey: "your_private_key_here",
    entityId: 12345,
    agentWalletAddress: "0x...", 
    config: Configurations.BaseSepoliaConfig, // or DefaultConfig
    onNewTask: async (job, memoToSign) => {
        Console.WriteLine($"New task received: {job.Id}");
        // Handle new task
    },
    onEvaluate: async (job) => {
        Console.WriteLine($"Evaluating job: {job.Id}");
        return (true, "Approved");
    },
    logger: logger
);

// Browse agents
var agents = await client.BrowseAgentsAsync("AI assistant");

// Initiate a job
var jobId = await client.InitiateJobAsync(
    providerAddress: "0x...",
    serviceRequirement: new { message = "Create a website" },
    amount: 100.0,
    evaluatorAddress: "0x...", // optional
    expiredAt: DateTime.UtcNow.AddDays(1) // optional
);

// Get active jobs
var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 10);

// Don't forget to dispose
client.Dispose();
```

## Configuration

The SDK supports multiple network configurations:

```csharp
// Base Sepolia (testnet)
var config = Configurations.BaseSepoliaConfig;

// Base Mainnet (production)
var config = Configurations.BaseMainnetConfig;

// Default (currently set to Base Mainnet)
var config = Configurations.DefaultConfig;
```

## Environment Variables

You can use environment variables for configuration:

```csharp
var envSettings = EnvSettings.FromEnvironment();
envSettings.ValidateWalletAddresses();
```

Supported environment variables:
- `WHITELISTED_WALLET_PRIVATE_KEY`
- `BUYER_AGENT_WALLET_ADDRESS`
- `SELLER_AGENT_WALLET_ADDRESS`
- `EVALUATOR_AGENT_WALLET_ADDRESS`
- `BUYER_ENTITY_ID`
- `SELLER_ENTITY_ID`
- `EVALUATOR_ENTITY_ID`

## Error Handling

The SDK provides specific exception types:

```csharp
try
{
    await client.InitiateJobAsync(...);
}
catch (AcpApiError ex)
{
    // API-related errors
    Console.WriteLine($"API Error: {ex.Message}");
}
catch (AcpContractError ex)
{
    // Blockchain contract errors
    Console.WriteLine($"Contract Error: {ex.Message}");
}
catch (AcpError ex)
{
    // General ACP errors
    Console.WriteLine($"ACP Error: {ex.Message}");
}
```

## Building and Testing

```bash
# Build the project
dotnet build

# Run tests (if any)
dotnet test

# Create NuGet package
dotnet pack
```