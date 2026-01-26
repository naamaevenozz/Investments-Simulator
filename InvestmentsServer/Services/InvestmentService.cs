using InvestmentsServer.Models;

namespace InvestmentsServer.Services;

/// <summary>
/// Manages all investments - user balance, available options and active investments.
/// This is the core service of the application.
/// </summary>
public class InvestmentService
{
    // Initial user balance
    private decimal _userBalance = 500m;

    // Currently active investments
    private readonly List<ActiveInvestment> _activeInvestments = new();

    // Available investment options
    private readonly List<InvestmentOption> _availableOptions;

    // Lock to prevent concurrency issues when accessed from multiple threads
    private readonly object _lock = new();

    /// <summary>
    /// Initializes the service with predefined investment options.
    /// </summary>
    public InvestmentService()
    {
        _availableOptions = new List<InvestmentOption>
        {
            new()
            {
                Name = "Short-term investment",
                Amount = 10,
                ExpectedReturn = 20,
                DurationInSeconds = 10
            },
            new()
            {
                Name = "Mid-term investment",
                Amount = 100,
                ExpectedReturn = 250,
                DurationInSeconds = 30
            },
            new()
            {
                Name = "Long-term investment",
                Amount = 1000,
                ExpectedReturn = 3000,
                DurationInSeconds = 60
            }
        };
    }

    /// <summary>
    /// Gets the current user balance.
    /// </summary>
    public decimal GetBalance()
    {
        lock (_lock)
        {
            return _userBalance;
        }
    }

    /// <summary>
    /// Returns the configured list of available investment options.
    /// </summary>
    public List<InvestmentOption> GetOptions() => _availableOptions;

    /// <summary>
    /// Returns a snapshot list of currently active investments.
    /// </summary>
    public List<ActiveInvestment> GetActiveInvestments()
    {
        lock (_lock)
        {
            return _activeInvestments.ToList();
        }
    }

    /// <summary>
    /// Attempts to start a new investment.
    /// Checks: enough funds and no existing active investment of the same type.
    /// </summary>
    /// <param name="optionName">The name of the investment option to start.</param>
    /// <returns>
    /// A tuple where Success indicates whether the start succeeded,
    /// and Message contains a human-readable result message.
    /// </returns>
    public (bool Success, string Message) StartInvestment(string optionName)
    {
        lock (_lock)
        {
            // Find the investment option
            var option = _availableOptions.FirstOrDefault(o => o.Name == optionName);

            if (option == null)
                return (false, "Investment option not found.");

            // Check for sufficient funds
            if (_userBalance < option.Amount)
                return (false, "Insufficient funds.");

            // Check if an active investment of the same type already exists
            if (_activeInvestments.Any(ai => ai.Name == optionName))
                return (false, "You already have an active investment of this type.");

            // Deduct funds from balance
            _userBalance -= option.Amount;

            // Create the new active investment
            var newInvestment = new ActiveInvestment
            {
                Name = option.Name,
                Amount = option.Amount,
                ExpectedReturn = option.ExpectedReturn,
                EndTime = DateTime.UtcNow.AddSeconds(option.DurationInSeconds)
            };

            // Add to active investments
            _activeInvestments.Add(newInvestment);

            return (true, "Investment started successfully.");
        }
    }

    /// <summary>
    /// Completes the active investment identified by <paramref name="id"/>,
    /// credits the expected return to the user's balance and removes the investment.
    /// Called by the BackgroundService when an investment finishes.
    /// </summary>
    /// <param name="id">The identifier of the active investment to complete.</param>
    public void CompleteInvestment(Guid id)
    {
        lock (_lock)
        {
            var investment = _activeInvestments.FirstOrDefault(i => i.Id == id);
            if (investment != null)
            {
                // Add the expected return to the balance
                _userBalance += investment.ExpectedReturn;

                // Remove from active list
                _activeInvestments.Remove(investment);
            }
        }
    }
}
