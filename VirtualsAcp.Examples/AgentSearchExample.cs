using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

/// <summary>
/// Example showing how to search and filter agents
/// </summary>
public class AgentSearchExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== Agent Search Example ===");

        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

        var client = new VirtualsACPClient(
            walletPrivateKey: "0000000000000000000000000000000000000000000000000000000000000001",
            entityId: 12345,
            config: Configurations.BaseSepoliaConfig,
            logger: logger
        );

        try
        {
            // Example 1: Basic search
            Console.WriteLine("Searching for 'AI' agents...");
            var aiAgents = await client.BrowseAgentsAsync("AI", topK: 10);
            Console.WriteLine($"Found {aiAgents.Count} AI agents");

            foreach (var agent in aiAgents.Take(3))
            {
                Console.WriteLine($"- {agent.Name}");
                Console.WriteLine($"  Wallet: {agent.WalletAddress}");
                Console.WriteLine($"  Description: {agent.Description}");
                Console.WriteLine($"  Offerings: {agent.Offerings.Count}");
            }

            // Example 2: Search with filters
            Console.WriteLine("\nSearching for graduated agents...");
            var graduatedAgents = await client.BrowseAgentsAsync(
                keyword: "developer",
                graduationStatus: AcpGraduationStatus.Graduated,
                topK: 5
            );
            Console.WriteLine($"Found {graduatedAgents.Count} graduated developer agents");

            // Example 3: Search with sorting
            Console.WriteLine("\nSearching for agents sorted by success rate...");
            var topAgents = await client.BrowseAgentsAsync(
                keyword: "web",
                sortBy: new List<AcpAgentSort> { AcpAgentSort.successRate, AcpAgentSort.successfulJobCount },
                topK: 5
            );
            Console.WriteLine($"Found {topAgents.Count} top web agents");

            // Example 4: Search with online status filter
            Console.WriteLine("\nSearching for online agents...");
            var onlineAgents = await client.BrowseAgentsAsync(
                keyword: "design",
                onlineStatus: AcpOnlineStatus.Online,
                topK: 5
            );
            Console.WriteLine($"Found {onlineAgents.Count} online design agents");

            // Example 5: Search with cluster filter
            Console.WriteLine("\nSearching for agents in specific cluster...");
            var clusterAgents = await client.BrowseAgentsAsync(
                keyword: "blockchain",
                cluster: "defi",
                topK: 5
            );
            Console.WriteLine($"Found {clusterAgents.Count} DeFi blockchain agents");

            // Example 6: Get specific agent details
            if (aiAgents.Any())
            {
                var agentAddress = aiAgents.First().WalletAddress;
                Console.WriteLine($"\nGetting details for agent: {agentAddress}");
                
                var agentDetails = await client.GetAgentAsync(agentAddress);
                if (agentDetails != null)
                {
                    Console.WriteLine($"Agent Details:");
                    Console.WriteLine($"  Name: {agentDetails.Name}");
                    Console.WriteLine($"  Description: {agentDetails.Description}");
                    Console.WriteLine($"  Twitter: {agentDetails.TwitterHandle}");
                    Console.WriteLine($"  Category: {agentDetails.Category}");
                    Console.WriteLine($"  Cluster: {agentDetails.Cluster}");
                    Console.WriteLine($"  Processing Time: {agentDetails.ProcessingTime}");
                    
                    if (agentDetails.Metrics != null)
                    {
                        Console.WriteLine("  Metrics:");
                        foreach (var metric in agentDetails.Metrics)
                        {
                            Console.WriteLine($"    {metric.Key}: {metric.Value}");
                        }
                    }

                    Console.WriteLine("  Offerings:");
                    foreach (var offering in agentDetails.Offerings)
                    {
                        Console.WriteLine($"    - {offering.Name}: ${offering.Price} (${offering.PriceUsd} USD)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Error in agent search example");
        }
        finally
        {
            client.Dispose();
        }
    }
}
