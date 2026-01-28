using Microsoft.AspNetCore.SignalR;

namespace InvestmentsServer.Hubs;

public class InvestmentHub : Hub
{
    private readonly ILogger<InvestmentHub> _logger;

    public InvestmentHub(ILogger<InvestmentHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

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

    public async Task UnsubscribeFromUpdates(string username)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
        
        _logger.LogInformation(
            "User {Username} unsubscribed from updates",
            username);
    }
    
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