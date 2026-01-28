using InvestmentsServer.Services;
using InvestmentsServer.Hubs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace InvestmentsServer.Services;

public class InvestmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvestmentBackgroundService> _logger;

    public InvestmentBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InvestmentBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Investment Completion Checker started - monitoring for completed investments...");

        // Wait a bit for the database to be ready
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var investmentService = scope.ServiceProvider.GetRequiredService<InvestmentService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<InvestmentHub>>(); // NEW!

                var now = DateTime.UtcNow;
                var allUsers = await investmentService.GetAllUsersAsync();

                foreach (var user in allUsers)
                {
                    var completedInvestments = user.ActiveInvestments
                        .Where(i => i.EndTime <= now)
                        .ToList();

                    foreach (var investment in completedInvestments)
                    {
                        try
                        {
                            // Complete the investment
                            await investmentService.CompleteInvestmentAsync(
                                user.Username,
                                investment.Id);

                            _logger.LogInformation(
                                "Investment completed: User={User}, Investment={Name}, Payout=${Return}",
                                user.Username,
                                investment.Name,
                                investment.ExpectedReturn);

                            // NEW: Send real-time notification via SignalR
                            var updatedUser = await investmentService.GetOrCreateUserAsync(user.Username);
                            
                            await hubContext.Clients.Group(user.Username).SendAsync(
                                "InvestmentCompleted",
                                new
                                {
                                    message = $"Investment '{investment.Name}' completed!",
                                    investmentId = investment.Id,
                                    investmentName = investment.Name,
                                    payout = investment.ExpectedReturn,
                                    newBalance = updatedUser.Balance,
                                    activeInvestments = updatedUser.ActiveInvestments
                                },
                                stoppingToken);

                            _logger.LogInformation(
                                "Completion notification sent to user {Username}",
                                user.Username);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error completing investment {InvestmentId} for user {User}",
                                investment.Id,
                                user.Username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while processing background investments");
            }

            // Check every second for completed investments
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Investment Completion Checker stopped");
    }
}