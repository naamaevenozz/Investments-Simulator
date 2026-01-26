using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentsServer.Services;

/// <summary>
/// Background service that runs continuously and checks for completed investments.
/// Acts like a timer that checks once per second.
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

    /// <summary>
    /// The main loop executed by the background service.
    /// Periodically checks active investments and completes those whose EndTime has passed.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Investment Background Service started - checking for completed investments");

        // Infinite loop runs while the host is not stopping
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
				// Use UtcNow to avoid timezone issues
                var now = DateTime.UtcNow;
                var activeInvestments = _investmentService.GetActiveInvestments();

                // Iterate over active investments
                foreach (var investment in activeInvestments)
                {
                    // If the investment has finished, complete it
                    if (investment.EndTime <= now)
                    {
                        _investmentService.CompleteInvestment(investment.Id);

                        _logger.LogInformation(
                            "Investment completed: {Name} (ID: {Id}), Return: ${Return}",
                            investment.Name,
                            investment.Id,
                            investment.ExpectedReturn);
                    }
                }
            }
			// Catch any exceptions to prevent the background service from crashing
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing investments");
            }

            // Wait one second before checking again - let the server take care other requests
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Investment Background Service stopped");
    }
}
