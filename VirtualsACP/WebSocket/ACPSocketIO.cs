using Microsoft.Extensions.Logging;
using SocketIOClient;
using System.Text.Json;

namespace VirtualsAcp.WebSocket;

public class ACPSocketIO : IDisposable
{
    private readonly SocketIOClient.SocketIO _client;
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public event Func<object, Task>? OnRoomJoined;
    public event Func<object, Task>? OnEvaluate;
    public event Func<object, Task>? OnNewTask;

    public ACPSocketIO(string socketUrl, ILogger? logger = null)
    {
        _logger = logger;

        // Convert HTTP API URL to Socket.IO URL if needed
        var socketIOUrl = ConvertToSocketIOUrl(socketUrl);

        _client = new SocketIOClient.SocketIO(socketIOUrl, new SocketIOOptions
        {
            // Use WebSocket transport (equivalent to transports=['websocket'] in Python)
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,

            // Enable automatic reconnection (equivalent to retry=True in Python)
            Reconnection = true,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelay = 1000,
            ReconnectionDelayMax = 5000,

            // Add SDK headers (equivalent to headers_data in Python)
            ExtraHeaders = new Dictionary<string, string>
            {
                ["x-sdk-version"] = VirtualsAcp.Version,
                ["x-sdk-language"] = "csharp"
            },
           
            ConnectionTimeout = TimeSpan.FromSeconds(20),

            // Enable all transports as fallback
            AutoUpgrade = true
        });

        SetupEventHandlers();
        SetupConnectionEvents();
    }

    private string ConvertToSocketIOUrl(string apiUrl)
    {
        // Convert from API URL to Socket.IO URL
        // From: https://acpx.virtuals.io/api
        // To: https://acpx.virtuals.io (remove /api suffix)

        if (apiUrl.EndsWith("/api"))
        {
            return apiUrl.Substring(0, apiUrl.Length - 4);
        }

        return apiUrl;
    }

    private void SetupEventHandlers()
    {
        // Equivalent to Python's self.sio.on('roomJoined', self._on_room_joined)
        _client.On("roomJoined", response =>
        {
            var data = response.GetValue<object>();
            _logger?.LogInformation("Connected to room: {Data}", JsonSerializer.Serialize(data));

            if (OnRoomJoined != null)
            {
                _ = Task.Run(async () => await OnRoomJoined(data));
            }
        });

        // Equivalent to Python's self.sio.on('onEvaluate', self._on_evaluate)
        _client.On("onEvaluate", response =>
        {
            var data = response.GetValue<object>();
            _logger?.LogInformation("Received evaluate event: {Data}", JsonSerializer.Serialize(data));

            if (OnEvaluate != null)
            {
                _ = Task.Run(async () => await OnEvaluate(data));
            }
        });

        // Equivalent to Python's self.sio.on('onNewTask', self._on_new_task)
        _client.On("onNewTask", response =>
        {
            var data = response.GetValue<object>();
            _logger?.LogInformation("Received new task event: {Data}", JsonSerializer.Serialize(data));

            if (OnNewTask != null)
            {
                _ = Task.Run(async () => await OnNewTask(data));
            }
        });
    }

    private void SetupConnectionEvents()
    {
        _client.OnConnected += async (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO connection established");
        };

        _client.OnDisconnected += async (sender, e) =>
        {
            _logger?.LogWarning("Socket.IO disconnected: {Reason}", e);
        };

        _client.OnReconnectAttempt += async (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO reconnection attempt: {Attempt}", e);
        };

        _client.OnReconnected += async (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO reconnected after {Attempt} attempts", e);
        };

        _client.OnError += async (sender, e) =>
        {
            _logger?.LogError("Socket.IO error: {Error}", e);
        };
    }

    public async Task StartAsync(string walletAddress, string? evaluatorAddress = null)
    {
        try
        {
            // Set authentication data (equivalent to auth_data in Python)
            var authData = new Dictionary<string, object>
            {
                ["walletAddress"] = walletAddress
            };

            if (!string.IsNullOrEmpty(evaluatorAddress))
            {
                authData["evaluatorAddress"] = evaluatorAddress;
            }

            // Set auth data before connecting
            _client.Options.Auth = authData;

            // Connect to the Socket.IO server
            await _client.ConnectAsync();
            _logger?.LogInformation("Socket.IO connection started for wallet: {WalletAddress}", walletAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start Socket.IO connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_client.Connected)
            {
                await _client.DisconnectAsync();
            }
            _logger?.LogInformation("Socket.IO connection stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Socket.IO connection");
        }
    }

    public async Task EmitAsync(string eventName, object data)
    {
        try
        {
            if (_client.Connected)
            {
                await _client.EmitAsync(eventName, data);
                _logger?.LogDebug("Emitted event: {EventName}", eventName);
            }
            else
            {
                _logger?.LogWarning("Cannot emit event {EventName}: not connected", eventName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to emit event: {EventName}", eventName);
            throw;
        }
    }

    public bool IsConnected => _client?.Connected ?? false;

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_client?.Connected == true)
                {
                    _client.DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
                }
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing Socket.IO client");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}