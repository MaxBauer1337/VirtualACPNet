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

        // variables
        string privateKeySeller = "0x1";
        string agentWalletSeller = "0x2";
        string privateKeyBuyer = "0x1";
        string agentWalletBuyer = "0x2";       


        // Initialize client with event handlers
        VirtualsACPClient provider = null;
        VirtualsACPClient client = null;
        provider = new VirtualsACPClient(
            walletPrivateKey: privateKeySeller,
            agentWalletAddress: agentWalletSeller,
            config: Configurations.BaseMainnetConfig,
            onNewTask: async (job, memoToSign) =>
            {
                LogTask(job, memoToSign);
               if (job.Phase == AcpJobPhase.Request && job.Memos.Any(x => x.NextPhase == AcpJobPhase.Negotiation))
                {
                    Console.WriteLine("\nResponding to job...");
                    var responseTxHash = await provider.RespondToJobAsync(
                        jobId: job.Id,
                        memoId: memoToSign.Id,
                        accept: true,
                        content: "I can complete this project within 3 days",
                        reason: "I have experience with similar projects"
                    );
                    Console.WriteLine($"✅ Job response sent: {responseTxHash}");
                }
                else if (job.Phase == AcpJobPhase.Transaction && job.Memos.Any(x => x.NextPhase == AcpJobPhase.Evaluation))
                {
                    Console.WriteLine("Delivering job");
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

                    var deliveryTxHash = await provider.DeliverJobAsync(job.Id, deliverable);
                    Console.WriteLine($"✅ Work delivered: {deliveryTxHash}");
                }
            },
            onEvaluate: async (job, memo) =>
            {
                // shouldn't be called on provider
                Console.WriteLine($"🔍 Evaluating job: {job.Id}");
                //Simulate evaluation logic
                await Task.Delay(1000);
                return (true, "Work completed successfully");
            },
            logger: logger
        );

        client = new VirtualsACPClient(
           walletPrivateKey: privateKeyBuyer,
           agentWalletAddress: agentWalletBuyer,
           config: Configurations.BaseMainnetConfig,
           onNewTask: async (job, memoToSign) =>
            {
                LogTask(job, memoToSign);
                if (job.Phase == AcpJobPhase.Negotiation && memoToSign?.NextPhase == AcpJobPhase.Transaction)
                {
                    var paymentResult = await client.PayJobAsync(
                        jobId: job.Id,
                        memoId: memoToSign.Id,
                        amount: 0.1m,
                        reason: "Payment for completed work"
                    );

                    Console.WriteLine($"✅ Payment processed: {paymentResult["txHash"]}");
                }
                else if (job.Phase == AcpJobPhase.Completed)
                {
                    Console.WriteLine($"Job completed: {job.Id}");
                }
                else if (job.Phase == AcpJobPhase.Rejected)
                {
                    Console.WriteLine($"Job rejected: {job.Id}");
                }
            },
            onEvaluate: async (job, memo) =>
            {
                Console.WriteLine($"🔍 Evaluating job: {job.Id}");

                var evaluationTxHash = await client.SignMemoAsync(
                    memoId: memo.Id,
                    accept: true,
                    reason: "Work meets all requirements and quality standards"
                );

                Console.WriteLine($"✅ Evaluation completed: {evaluationTxHash}");
                return (true, "Work completed successfully");
            },
           logger: logger
       );

        try
        {
            await provider.StartAsync();
            await client.StartAsync();

            // Example 1: Create a job
            Console.WriteLine("Creating a new job...");
            var jobId = await client.InitiateJobAsync(
                providerAddress: agentWalletSeller,
                serviceRequirement: new
                {
                    message = "Create a simple website with contact form",
                    requirements = new
                    {
                        pages = new[] { "home", "about", "contact" },
                        features = new[] { "responsive", "contact form" }
                    }
                },
                amount: 0.1m,
                expiredAt: DateTime.UtcNow.AddDays(7)
            );

            Console.WriteLine($"✅ Job created with ID: {jobId}");

            await Task.Delay(10_000); // wait else memos might be empty
            // wait indefinetly, rest should be handled in event han
            Console.WriteLine("Waiting indefinetly, all logic should be handled by events");
            Console.ReadLine();
            return;          
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Error in job management example");
        }
        finally
        {
            provider.Dispose();
            client.Dispose();
        }
    }

    private static void LogTask(ACPJob job, ACPMemo? memoToSign)
    {
        Console.WriteLine($"📨 New task received: Job {job.Id}");
        Console.WriteLine($"   Service: {job.ServiceName}");
        Console.WriteLine($"   Provider: {job.ProviderAddress}");
        Console.WriteLine($"   Price: {job.Price}");
        Console.WriteLine($"   Phase: {job.Phase}");
        Console.WriteLine($"   Memos: {job.Memos.Count}");

        if (memoToSign != null)
            Console.WriteLine($"   Memo to sign: {memoToSign.Id}");
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
