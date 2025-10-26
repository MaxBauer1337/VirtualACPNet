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

        // ⚠️ CONFIGURATION REQUIRED:
        // Set the DegenAI seller address (where to send jobs)
        
        // Provider (seller) - DegenAI agent address (running separately)
        // We only need the ADDRESS to send jobs to, not the private key
        string agentWalletSeller = "0xDf15aF9E38d713E5eF207D1b54b91b1dBBE6cC29"; // Your ACP Agent Address from DegenAI Settings (/admin/settings)
        
        // Requester (buyer) - Test requester credentials (pre-configured)
        string privateKeyBuyer = "YOUR_PRIVATE_KEY_HERE"; // ⚠️ Replace with your actual private key
        string agentWalletBuyer = "YOUR_WALLET_ADDRESS_HERE"; // ⚠️ Replace with your actual wallet address

        // Validate configuration
        if (string.IsNullOrWhiteSpace(agentWalletSeller))
        {
            Console.WriteLine("❌ ERROR: DegenAI seller address not set!");
            Console.WriteLine("Please set agentWalletSeller in JobManagementExample.cs (line 26)");
            Console.WriteLine("Use your ACP Agent Address from DegenAI Settings page (/admin/settings)");
            Console.WriteLine("\nExample: agentWalletSeller = \"0x1234...\";");
            return;
        }

        Console.WriteLine($"✅ Sending jobs to DegenAI: {agentWalletSeller}");
        Console.WriteLine($"✅ Using test buyer: {agentWalletBuyer}");     


        // Initialize client with event handlers
        // COMMENTED OUT: DegenAI is the actual seller/provider
        // We only need the buyer/client in this example to avoid wallet conflicts
        // VirtualsACPClient provider = null;
        VirtualsACPClient client = null;
        
        /* PROVIDER COMMENTED OUT - DegenAI is the seller
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
        */

        client = new VirtualsACPClient(
           walletPrivateKey: privateKeyBuyer,
           agentWalletAddress: agentWalletBuyer,
           config: Configurations.BaseMainnetConfig,
           onNewTask: async (job, memoToSign) =>
            {
                Console.WriteLine($"\n[DEBUG] ━━━ onNewTask FIRED ━━━");
                Console.WriteLine($"[DEBUG] Job={job.Id}, Phase={job.Phase}, MemoToSign={memoToSign?.Id}");
                Console.WriteLine($"[DEBUG] MemoType={memoToSign?.Type}, NextPhase={memoToSign?.NextPhase}");
                Console.WriteLine($"[DEBUG] Total Memos={job.Memos.Count}");
                
                // NEW: Log ALL memos to see complete history
                Console.WriteLine($"[DEBUG] ━━━ ALL MEMOS ━━━");
                foreach (var memo in job.Memos)
                {
                    Console.WriteLine($"[DEBUG]   Memo {memo.Id}: Type={memo.Type}, Status={memo.Status}, NextPhase={memo.NextPhase}");
                }
                
                // NEW: Check if there's a DELIVER_SERVICE memo we should be handling
                var deliveryMemo = job.Memos.FirstOrDefault(m => m.Type == "DELIVER_SERVICE");
                if (deliveryMemo != null)
                {
                    Console.WriteLine($"[DEBUG] ⚠️ FOUND DELIVER_SERVICE MEMO: Id={deliveryMemo.Id}, Status={deliveryMemo.Status}, NextPhase={deliveryMemo.NextPhase}");
                    Console.WriteLine($"[DEBUG] ⚠️ memoToSign matches DELIVER_SERVICE? {memoToSign?.Id == deliveryMemo.Id}");
                }
                
                LogTask(job, memoToSign);
                
                if (job.Phase == AcpJobPhase.Negotiation && memoToSign?.NextPhase == AcpJobPhase.Transaction)
                {
                    Console.WriteLine($"[DEBUG] 💰 Payment handler triggered");
                    var paymentResult = await client.PayJobAsync(
                        jobId: job.Id,
                        memoId: memoToSign.Id,
                        amount: 0.1m,
                        reason: "Payment for completed work"
                    );

                    Console.WriteLine($"[DEBUG] ✅ Payment completed: {paymentResult["txHash"]}");
                    Console.WriteLine($"✅ Payment processed: {paymentResult["txHash"]}");
                }
                else if (memoToSign?.Type == "DELIVER_SERVICE" && memoToSign?.NextPhase == AcpJobPhase.Completed)
                {
                    Console.WriteLine($"[DEBUG] 📦 Delivery approval handler triggered (DELIVER_SERVICE with nextPhase=4)");
                    Console.WriteLine($"[BUYER] Approving delivery for job {job.Id}");
                    
                    await client.SignMemoAsync(
                        memoId: memoToSign.Id,
                        accept: true,
                        reason: "Delivery approved, job complete"
                    );
                    
                    Console.WriteLine($"[BUYER] ✅ Delivery approved, job moving to completion");
                }
                /* OLD PATTERN - No longer used with DELIVER_SERVICE memos
                else if (job.Memos.Any(m => m.Type == "REQUEST_EVALUATION" && m.Status == "APPROVED") &&
                         job.Phase != AcpJobPhase.Completed)
                {
                    // This handler was for REQUEST_EVALUATION memos with nextPhase=3
                    // Now we use DELIVER_SERVICE memos with nextPhase=4 that go directly to completion
                }
                */
                else if (job.Phase == AcpJobPhase.Completed)
                {
                    Console.WriteLine($"[BUYER] ✅ Job completed: {job.Id}");
                }
                else if (job.Phase == AcpJobPhase.Rejected)
                {
                    Console.WriteLine($"\n[BUYER] ❌ Job {job.Id} was REJECTED");
                    
                    var rejectedMemo = job.Memos.FirstOrDefault(m => m.Status == "REJECTED");
                    if (rejectedMemo != null)
                    {
                        Console.WriteLine($"[BUYER] Rejection reason: {rejectedMemo.SignedReason}");
                        if (!string.IsNullOrWhiteSpace(rejectedMemo.Content))
                        {
                            Console.WriteLine($"\n[BUYER] Butler/Buyer agent would see:\n{rejectedMemo.Content}\n");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ⚠️ No handler matched - Phase={job.Phase}, MemoType={memoToSign?.Type}, NextPhase={memoToSign?.NextPhase}");
                    Console.WriteLine($"[DEBUG] ⚠️ Handlers available: Payment (Phase=Negotiation, NextPhase=Transaction), DELIVER_SERVICE (Type=DELIVER_SERVICE, NextPhase=Completed), Completed, Rejected");
                }
            },
            onEvaluate: async (job, memo) =>
            {
                // ⚠️ NOTE: This callback will NOT fire in self-evaluation scenarios (buyer = evaluator).
                // In ACP v2, self-evaluation is handled in onNewTask when Phase=3.
                // This callback only fires for EXTERNAL evaluation scenarios (when buyer ≠ evaluator).
                // Reference: https://github.com/Virtual-Protocol/acp-node/pull/82/files (Oct 9, 2025)
                
                Console.WriteLine($"\n[DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"[DEBUG] ⭐⭐⭐ onEvaluate FIRED (External Evaluator Scenario) ⭐⭐⭐");
                Console.WriteLine($"[DEBUG] Job={job.Id}, Phase={job.Phase}, MemoId={memo?.Id}");
                Console.WriteLine($"[DEBUG] MemoType={memo?.Type}, MemoStatus={memo?.Status}");
                Console.WriteLine($"[DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"[BUYER] Evaluating delivered job {job.Id}");
                
                // Simulate evaluation (checking deliverable quality, etc.)
                await Task.Delay(1000);
                
                // Auto-approve all jobs
                // SDK will automatically call SignMemoAsync with our result
                Console.WriteLine($"[BUYER] ✅ Auto-approving job {job.Id}");
                Console.WriteLine($"[DEBUG] ✅ onEvaluate returning: accept=true");
                return (true, "Job delivered successfully, payment approved");
            },
           logger: logger
       );

        try
        {
            // await provider.StartAsync(); // DegenAI is the seller
            
            // Start client with evaluatorAddress to receive onEvaluate events
            // In self-evaluation scenarios (buyer = evaluator), this is required for onEvaluate to fire
            await client.StartAsync(agentWalletBuyer, evaluatorAddress: agentWalletBuyer);

            // Example 1: Create a job (Real trading request)
            Console.WriteLine("Creating a new job...");
            var jobId = await client.InitiateJobAsync(
                providerAddress: agentWalletSeller,
                serviceRequirement: "Long BTC at 105k with $100 total. Add take profit at 108k and stop loss at 103k",
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
            // provider.Dispose(); // DegenAI is the seller
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
