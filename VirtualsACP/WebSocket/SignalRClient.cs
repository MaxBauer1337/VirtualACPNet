using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace VirtualsAcp.WebSocket;

public class SignalRClient : IDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public event Func<object, Task>? OnRoomJoined;
    public event Func<object, Task>? OnEvaluate;
    public event Func<object, Task>? OnNewTask;

    public SignalRClient(string hubUrl, ILogger? logger = null)
    {
        _logger = logger;
        
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                if (logger != null)
                {
                    logging.AddProvider(new LoggerProviderWrapper(logger));
                }
            })
            .Build();

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        _connection.On<object>("roomJoined", async (data) =>
        {
            _logger?.LogInformation("Connected to room: {Data}", JsonSerializer.Serialize(data));
            if (OnRoomJoined != null)
            {
                await OnRoomJoined(data);
            }
        });

        _connection.On<object>("onEvaluate", async (data) =>
        {
            _logger?.LogInformation("Received evaluate event: {Data}", JsonSerializer.Serialize(data));
            if (OnEvaluate != null)
            {
                await OnEvaluate(data);
            }
        });

        _connection.On<object>("onNewTask", async (data) =>
        {
            _logger?.LogInformation("Received new task event: {Data}", JsonSerializer.Serialize(data));
            if (OnNewTask != null)
            {
                await OnNewTask(data);
            }
        });
    }

    public async Task StartAsync()
    {
        try
        {
            await _connection.StartAsync();
            _logger?.LogInformation("SignalR connection started");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _connection.StopAsync();
            _logger?.LogInformation("SignalR connection stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping SignalR connection");
        }
    }

    public async Task JoinRoomAsync(string walletAddress, string? evaluatorAddress = null)
    {
        try
        {
            var authData = new Dictionary<string, object>
            {
                ["walletAddress"] = walletAddress
            };

            if (!string.IsNullOrEmpty(evaluatorAddress))
            {
                authData["evaluatorAddress"] = evaluatorAddress;
            }

            await _connection.InvokeAsync("JoinRoom", authData);
            _logger?.LogInformation("Joined room for wallet: {WalletAddress}", walletAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to join room for wallet: {WalletAddress}", walletAddress);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.DisposeAsync().AsTask().Wait();
            _disposed = true;
        }
    }

    private class LoggerProviderWrapper : ILoggerProvider
    {
        private readonly ILogger _logger;

        public LoggerProviderWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
