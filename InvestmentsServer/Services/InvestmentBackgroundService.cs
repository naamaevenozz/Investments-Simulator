using InvestmentsServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentsServer.Services;

/// <summary>
/// Background service that checks ALL users for completed investments.
/// Runs once per second to ensure timely payouts.
/// </summary>
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
        _logger.LogInformation("Global Investment Background Service started.");

        // Wait a bit for the database to be ready
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var investmentService =
                    scope.ServiceProvider.GetRequiredService<InvestmentService>();

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
                            await investmentService.CompleteInvestmentAsync(
                                user.Username,
                                investment.Id);

                            _logger.LogInformation(
                                "SUCCESS: User {User} - Investment {Name} completed. Payout: ${Return}",
                                user.Username,
                                investment.Name,
                                investment.ExpectedReturn);
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
                    "Error occurred while processing global background investments.");
            }

            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Global Investment Background Service stopped.");
    }
}
