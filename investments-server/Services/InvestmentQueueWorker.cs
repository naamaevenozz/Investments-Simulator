using InvestmentsServer.Hubs;
using InvestmentsServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace InvestmentsServer.Services;

/// <summary>
/// Background worker that listens to the investment queue
/// and processes investment requests asynchronously
/// This is the core of the Event-Driven Architecture
/// </summary>
public class InvestmentQueueWorker : BackgroundService
{
    private readonly InvestmentQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvestmentQueueWorker> _logger;

    public InvestmentQueueWorker(
        InvestmentQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<InvestmentQueueWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Investment Queue Worker started - listening for investment requests...");

        // Small delay to ensure services are ready
        await Task.Delay(1000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // This is a BLOCKING call - waits until a request arrives in the queue
                var request = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Processing investment request from queue: User={Username}, Option={OptionName}",
                    request.Username,
                    request.OptionName);

                // Process the investment in a separate scope
                await ProcessInvestmentRequestAsync(request);
            }
            catch (OperationCanceledException)
            {
                // Service is shutting down - this is expected
                _logger.LogInformation("Investment Queue Worker is shutting down...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing investment request from queue");
                
                // Continue processing other requests even if one fails
            }
        }

        _logger.LogInformation("Investment Queue Worker stopped");
    }

    /// <summary>
    /// Processes a single investment request from the queue
    /// </summary>
    private async Task ProcessInvestmentRequestAsync(InvestRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var investmentService = scope.ServiceProvider.GetRequiredService<InvestmentService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<InvestmentHub>>();

        try
        {
            // Start the investment (validates and adds to DB)
            await investmentService.StartInvestmentAsync(request.Username, request.OptionName);

            _logger.LogInformation(
                "Investment started successfully: User={Username}, Option={OptionName}",
                request.Username,
                request.OptionName);

            // Get updated user data
            var user = await investmentService.GetOrCreateUserAsync(request.Username);

            // Push real-time update to the user's frontend via SignalR
            await hubContext.Clients.Group(request.Username).SendAsync(
                "InvestmentStarted",
                new
                {
                    message = "Investment started successfully!",
                    optionName = request.OptionName,
                    newBalance = user.Balance,
                    activeInvestments = user.ActiveInvestments
                });

            _logger.LogInformation(
                "Real-time update sent to user {Username}",
                request.Username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Investment failed: User={Username}, Error={Error}",
                request.Username,
                ex.Message);

            // Send error notification to frontend
            await hubContext.Clients.Group(request.Username).SendAsync(
                "InvestmentFailed",
                new
                {
                    message = ex.Message,
                    optionName = request.OptionName
                });
        }
    }
}