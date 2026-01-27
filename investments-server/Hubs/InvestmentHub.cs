using Microsoft.AspNetCore.SignalR;

namespace InvestmentsServer.Hubs;

/// <summary>
/// SignalR Hub for real-time investment updates
/// Allows the backend to push updates to connected clients
/// </summary>
public class InvestmentHub : Hub
{
    private readonly ILogger<InvestmentHub> _logger;

    public InvestmentHub(ILogger<InvestmentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Subscribes a user to their personal update channel
    /// Creates a SignalR group named after the username
    /// </summary>
    public async Task SubscribeToUpdates(string username)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, username);
        
        _logger.LogInformation(
            "User {Username} subscribed to updates (ConnectionId: {ConnectionId})",
            username,
            Context.ConnectionId);
        
        // Send confirmation back to client
        await Clients.Caller.SendAsync("SubscriptionConfirmed", new 
        { 
            username, 
            message = "Connected to real-time updates" 
        });
    }

    /// <summary>
    /// Unsubscribes a user from their update channel
    /// </summary>
    public async Task UnsubscribeFromUpdates(string username)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
        
        _logger.LogInformation(
            "User {Username} unsubscribed from updates",
            username);
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected: {ConnectionId}",
                Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}