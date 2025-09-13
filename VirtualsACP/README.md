# VirtualsACP C# SDK

A C# SDK for the Virtuals Agent Contract Protocol (ACP), converted from the original Python implementation.

## Features

- **Blockchain Integration**: Uses Nethereum for Ethereum blockchain interactions
- **WebSocket Support**: Real-time communication using ASP.NET Core SignalR
- **REST API Client**: HTTP client for ACP API interactions
- **Type Safety**: Strongly typed models and enums
- **Async/Await**: Full async support throughout the SDK
- **Logging**: Built-in logging support with Microsoft.Extensions.Logging

## Project Structure

```
VirtualsAcp/
├── Models/                 # Data models and enums
│   ├── Enums.cs           # All enumeration types
│   ├── IDeliverable.cs    # Deliverable interface
│   ├── IACPAgent.cs       # Agent model
│   ├── ACPJob.cs          # Job model
│   ├── ACPMemo.cs         # Memo model
│   ├── ACPJobOffering.cs  # Job offering model
│   └── PayloadModels.cs   # Payload models for different operations
├── Configs/               # Configuration classes
│   ├── AcpContractConfig.cs # Contract configuration
│   └── EnvSettings.cs     # Environment settings
├── Exceptions/            # Custom exceptions
│   └── AcpExceptions.cs   # ACP-specific exceptions
├── Blockchain/            # Blockchain integration
│   └── NethereumBlockchainClient.cs # Nethereum-based client
├── Utils/                 # Utility classes
│   ├── JsonUtils.cs       # JSON parsing utilities
│   └── DateTimeExtensions.cs # DateTime extensions
├── Abi/                   # Contract ABIs
│   └── ContractAbis.cs    # ACP and ERC20 contract ABIs
├── WebSocket/             # WebSocket communication
│   └── SignalRClient.cs   # SignalR client implementation
├── Http/                  # HTTP client
│   └── AcpApiClient.cs    # REST API client
├── Examples/              # Example usage
│   └── Program.cs         # Example program
├── VirtualsACPClient.cs   # Main client class
├── VirtualsAcp.cs         # Namespace and version info
└── VirtualsAcp.csproj     # Project file
```

## Key Differences from Python Version

### 1. **Blockchain Client**
- **Python**: Used Alchemy SDK for blockchain interactions
- **C#**: Uses Nethereum library for direct Ethereum integration
- **Benefits**: More control, better performance, no external dependencies

### 2. **WebSocket Communication**
- **Python**: Used socketio-client
- **C#**: Uses ASP.NET Core SignalR
- **Benefits**: Better integration with .NET ecosystem, automatic reconnection

### 3. **HTTP Client**
- **Python**: Used requests library
- **C#**: Uses HttpClient with proper async patterns
- **Benefits**: Better resource management, built-in retry policies

### 4. **Data Models**
- **Python**: Used Pydantic for validation
- **C#**: Uses System.Text.Json with JsonPropertyName attributes
- **Benefits**: Better performance, native .NET serialization

### 5. **Configuration**
- **Python**: Used dataclasses and environment variables
- **C#**: Uses classes with properties and environment variable parsing
- **Benefits**: Type safety, better IDE support

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
    agentWalletAddress: "0x...", // optional
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

## Dependencies

- **.NET 9.0**: Target framework
- **Nethereum**: Ethereum blockchain integration
- **Microsoft.AspNetCore.SignalR.Client**: WebSocket communication
- **System.Text.Json**: JSON serialization
- **Newtonsoft.Json**: Additional JSON utilities
- **Microsoft.Extensions.Logging**: Logging framework
- **Microsoft.Extensions.Http**: HTTP client extensions
- **Microsoft.Extensions.DependencyInjection**: Dependency injection

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

## Migration from Python

If you're migrating from the Python version:

1. **Async/Await**: All methods are now async and return `Task` or `Task<T>`
2. **Disposal**: Remember to call `Dispose()` on the client when done
3. **Event Handlers**: Use C# delegates instead of Python callbacks
4. **Configuration**: Use the `Configurations` class instead of importing configs
5. **Error Handling**: Use specific exception types instead of generic exceptions

## License

This project maintains the same license as the original Python implementation.
