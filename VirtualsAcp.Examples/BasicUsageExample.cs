using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

/// <summary>
/// Basic usage example showing how to initialize and use the VirtualsACP client
/// </summary>
public class BasicUsageExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== Basic Usage Example ===");

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

        // Initialize client with minimal configuration
        var client = new VirtualsACPClient(
            walletPrivateKey: "0000000000000000000000000000000000000000000000000000000000000001",
            entityId: 12345,
            config: Configurations.BaseSepoliaConfig,
            logger: logger
        );

        try
        {
            // Browse for agents
            Console.WriteLine("Searching for AI agents...");
            var agents = await client.BrowseAgentsAsync("AI assistant", topK: 5);
            
            foreach (var agent in agents)
            {
                Console.WriteLine($"- {agent.Name} ({agent.WalletAddress})");
                Console.WriteLine($"  Description: {agent.Description}");
                Console.WriteLine($"  Offerings: {agent.Offerings.Count}");
            }

            // Get active jobs
            Console.WriteLine("\nFetching active jobs...");
            var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 5);
            Console.WriteLine($"Found {activeJobs.Count} active jobs");

            foreach (var job in activeJobs)
            {
                Console.WriteLine($"- Job {job.Id}: {job.ServiceName}");
                Console.WriteLine($"  Provider: {job.ProviderAddress}");
                Console.WriteLine($"  Price: {job.Price}");
                Console.WriteLine($"  Phase: {job.Phase}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }
}
