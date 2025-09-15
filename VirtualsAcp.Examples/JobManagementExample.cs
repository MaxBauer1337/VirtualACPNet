using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

/// <summary>
/// Example showing how to manage jobs (create, respond, pay, deliver)
/// </summary>
public class JobManagementExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== Job Management Example ===");

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

        // Initialize client with event handlers
        var provider = new VirtualsACPClient(
            walletPrivateKey: "1",
            agentWalletAddress: "0x0987654321098765432109876543210987654321",
            entityId: 12345,
            config: Configurations.BaseMainnetConfig,
            onNewTask: async (job, memoToSign) =>
            {
                Console.WriteLine($"📨 New task received: Job {job.Id}");
                Console.WriteLine($"   Service: {job.ServiceName}");
                Console.WriteLine($"   Provider: {job.ProviderAddress}");
                Console.WriteLine($"   Price: {job.Price}");

                if (memoToSign != null)
                {
                    Console.WriteLine($"   Memo to sign: {memoToSign.Id}");
                }
            },
            onEvaluate: async (job) =>
            {
                Console.WriteLine($"🔍 Evaluating job: {job.Id}");
                //Simulate evaluation logic
                await Task.Delay(1000);
                return (true, "Work completed successfully");
            },
            logger: logger
        );

        var client = new VirtualsACPClient(
           walletPrivateKey: "1",
           entityId: 12346,
           config: Configurations.BaseMainnetConfig,          
           logger: logger
       );

        try
        {
            await provider.StartAsync();

            // Example 1: Create a job
            Console.WriteLine("Creating a new job...");
            var jobId = await client.InitiateJobAsync(
                providerAddress: "0x0987654321098765432109876543210987654321",
                serviceRequirement: new
                {
                    message = "Create a simple website with contact form",
                    requirements = new
                    {
                        pages = new[] { "home", "about", "contact" },
                        features = new[] { "responsive", "contact form" }
                    }
                },
                amount: 0.1,
                //evaluatorAddress: "0x0987654321098765432109876543210987654321",
                expiredAt: DateTime.UtcNow.AddDays(7)
            );

            Console.WriteLine($"✅ Job created with ID: {jobId}");

            await Task.Delay(10_000); // wait else memos might be empty
            //Console.ReadLine();

            // Example 2: Get job details
            Console.WriteLine("\nFetching job details...");
            ACPJob? job = await RefreshJob(provider, jobId);

            // Example 3: Respond to a job (as a provider)
            Console.WriteLine("\nResponding to job...");
            var responseTxHash = await provider.RespondToJobAsync(
                jobId: jobId,
                memoId: job.Memos.First().Id,
                accept: true,
                content: "I can complete this project within 3 days",
                reason: "I have experience with similar projects"
            );

            Console.WriteLine($"✅ Job response sent: {responseTxHash}");

            await Task.Delay(10_000);

            Console.ReadLine();

            // Example 4: Pay for a job (as a client)
            Console.WriteLine("\nPaying for job...");
            job = await RefreshJob(client, jobId);
            var paymentResult = await client.PayJobAsync(
                jobId: jobId,
                memoId: job.Memos[1].Id,
                amount: 0.1,
                reason: "Payment for completed work"
            );

            Console.WriteLine($"✅ Payment processed: {paymentResult["txHash"]}");

            await Task.Delay(10_000);

            // Example 5: Deliver work (as a provider)
            Console.WriteLine("\nDelivering work...");
            var deliverable = new Deliverable
            {
                Type = "website",
                Value = new
                {
                    url = "https://example.com/website",
                    files = new[] { "index.html", "style.css", "script.js" },
                    description = "Responsive website with contact form"
                }
            };

            var deliveryTxHash = await provider.DeliverJobAsync(jobId, deliverable);
            Console.WriteLine($"✅ Work delivered: {deliveryTxHash}");

            await Task.Delay(10_000);

            job = await RefreshJob(client, jobId);

            // Example 6: Evaluate delivery (as an evaluator)
            Console.WriteLine("\nEvaluating delivery...");
            var evaluationTxHash = await client.SignMemoAsync(
                memoId: job.Memos[2].Id,
                accept: true,
                reason: "Work meets all requirements and quality standards"
            );

            Console.WriteLine($"✅ Evaluation completed: {evaluationTxHash}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Error in job management example");
        }
        finally
        {
            provider.Dispose();
        }
    }

    private static async Task<ACPJob?> RefreshJob(VirtualsACPClient client, int jobId)
    {
        var job = await client.GetJobByIdAsync(jobId);
        if (job != null)
        {
            Console.WriteLine($"Job {job.Id} details:");
            Console.WriteLine($"  Status: {job.Phase}");
            Console.WriteLine($"  Provider: {job.ProviderAddress}");
            Console.WriteLine($"  Client: {job.ClientAddress}");
            Console.WriteLine($"  Evaluator: {job.EvaluatorAddress}");
            Console.WriteLine($"  Price: {job.Price}");
            Console.WriteLine($"  Memos: {job.Memos.Count}");
        }

        return job;
    }
}
