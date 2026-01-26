using InvestmentsServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentsServer.Services;

/// <summary>
/// Background service that checks ALL users for completed investments.
/// Runs once per second to ensure timely payouts.
/// </summary>
public class InvestmentBackgroundService : BackgroundService
{
    private readonly InvestmentService _investmentService;
    private readonly ILogger<InvestmentBackgroundService> _logger;

    public InvestmentBackgroundService(
        InvestmentService investmentService, 
        ILogger<InvestmentBackgroundService> logger)
    {
        _investmentService = investmentService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Global Investment Background Service started.");

        // Infinite loop that runs as long as the server is operational
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Retrieve all users currently existing in the system
                var allUsers = _investmentService.GetAllUsers();

                foreach (var user in allUsers)
                {
                    // Find all investments for the current user where the end time has passed
                    var completedInvestments = user.activeInvestments
                        .Where(i => i.EndTime <= now)
                        .ToList();

                    foreach (var investment in completedInvestments)
                    {
                        // Finalize the investment and update the balance for the specific user
                        _investmentService.CompleteInvestment(user.username, investment.Id);

                        _logger.LogInformation(
                            "SUCCESS: User {User} - Investment {Name} completed. Payout: ${Return}",
                            user.username,
                            investment.Name,
                            investment.ExpectedReturn);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing global background investments.");
            }

            // Wait for one second before the next scan
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Global Investment Background Service stopped.");
    }
}