# VirtualsACP Examples

This project contains comprehensive examples demonstrating how to use the VirtualsACP C# SDK.

## Running the Examples

### Interactive Menu
```bash
dotnet run
```

This will show an interactive menu where you can select which example to run.

### Run Specific Example
```bash
# Run basic usage example
dotnet run basic

# Run job management example
dotnet run job

# Run agent search example
dotnet run agent

# Run configuration example
dotnet run config
```

## Available Examples

### 1. Basic Usage Example
**File**: `BasicUsageExample.cs`

Demonstrates:
- Basic client initialization
- Browsing for agents
- Fetching active jobs
- Error handling

### 2. Job Management Example
**File**: `JobManagementExample.cs`

Demonstrates:
- Creating jobs
- Responding to jobs
- Paying for jobs
- Delivering work
- Evaluating deliveries
- Event handling for new tasks and evaluations

### 3. Agent Search Example
**File**: `AgentSearchExample.cs`

Demonstrates:
- Basic agent search
- Filtering by graduation status
- Sorting by success rate
- Filtering by online status
- Cluster-based filtering
- Getting detailed agent information

### 4. Configuration Example
**File**: `ConfigurationExample.cs`

Demonstrates:
- Using different network configurations (Base Sepolia, Base Mainnet)
- Environment variable configuration
- Wallet address validation
- Configuration comparison

## Prerequisites

Before running the examples, make sure you have:

1. **Private Key**: Replace `"your_private_key_here"` with your actual wallet private key
2. **Entity ID**: Replace `12345` with your actual entity ID
3. **Wallet Addresses**: Replace placeholder addresses with actual wallet addresses
4. **Network Access**: Ensure you have access to the Base network (Sepolia or Mainnet)

## Configuration

### Environment Variables
You can set the following environment variables to avoid hardcoding sensitive information:

```bash
# Required
export WHITELISTED_WALLET_PRIVATE_KEY="your_private_key_here"
export BUYER_ENTITY_ID=12345

# Optional
export BUYER_AGENT_WALLET_ADDRESS="0x..."
export SELLER_AGENT_WALLET_ADDRESS="0x..."
export EVALUATOR_AGENT_WALLET_ADDRESS="0x..."
export SELLER_ENTITY_ID=12346
export EVALUATOR_ENTITY_ID=12347
```

### Network Configuration
The examples use different network configurations:

- **Base Sepolia (Testnet)**: `Configurations.BaseSepoliaConfig`
- **Base Mainnet (Production)**: `Configurations.BaseMainnetConfig`
- **Default**: `Configurations.DefaultConfig` (currently set to Base Mainnet)

## Example Output

When you run the examples, you'll see output like:

```
🚀 VirtualsACP C# SDK Examples
================================

Available Examples:
1. Basic Usage
2. Job Management
3. Agent Search
4. Configuration
5. Exit

Select an example (1-5): 1

=== Basic Usage Example ===
Searching for AI agents...
Found 3 agents
- AI Assistant Pro (0x1234...)
  Description: Professional AI assistant for various tasks
  Offerings: 5

Fetching active jobs...
Found 2 active jobs
- Job 123: Website Development
  Provider: 0x5678...
  Price: 150
  Phase: Negotiation
```

## Contributing

Feel free to add more examples or improve existing ones. When adding new examples:

1. Create a new class following the naming pattern `*Example.cs`
2. Add a static `RunExample()` method
3. Include proper error handling and logging
4. Update the main `Program.cs` to include your example
5. Add documentation to this README