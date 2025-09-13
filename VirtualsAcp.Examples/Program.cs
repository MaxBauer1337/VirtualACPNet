using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 VirtualsACP C# SDK Examples");
        Console.WriteLine("================================");
        Console.WriteLine();

        if (args.Length > 0)
        {
            await RunSpecificExample(args[0]);
        }
        else
        {
            await ShowMenu();
        }
    }

    private static async Task ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Available Examples:");
            Console.WriteLine("1. Basic Usage");
            Console.WriteLine("2. Job Management");
            Console.WriteLine("3. Agent Search");
            Console.WriteLine("4. Configuration");
            Console.WriteLine("5. Exit");
            Console.WriteLine();
            Console.Write("Select an example (1-5): ");

            var input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    await BasicUsageExample.RunExample();
                    break;
                case "2":
                    await JobManagementExample.RunExample();
                    break;
                case "3":
                    await AgentSearchExample.RunExample();
                    break;
                case "4":
                    await ConfigurationExample.RunExample();
                    break;
                case "5":
                    Console.WriteLine("Goodbye! 👋");
                    return;
                default:
                    Console.WriteLine("Invalid selection. Please try again.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private static async Task RunSpecificExample(string exampleName)
    {
        switch (exampleName.ToLower())
        {
            case "basic":
            case "1":
                await BasicUsageExample.RunExample();
                break;
            case "job":
            case "jobs":
            case "2":
                await JobManagementExample.RunExample();
                break;
            case "agent":
            case "agents":
            case "search":
            case "3":
                await AgentSearchExample.RunExample();
                break;
            case "config":
            case "configuration":
            case "4":
                await ConfigurationExample.RunExample();
                break;
            default:
                Console.WriteLine($"Unknown example: {exampleName}");
                Console.WriteLine("Available examples: basic, job, agent, config");
                break;
        }
    }
}
