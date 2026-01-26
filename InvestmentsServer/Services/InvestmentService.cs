using InvestmentsServer.Models;
using InvestmentsServer.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestmentsServer.Services;

/// <summary>
/// Service for managing investments with database persistence
/// </summary>
public class InvestmentService
{
    private readonly IDbContextFactory<InvestmentDbContext> _contextFactory;
    private readonly ILogger<InvestmentService> _logger;

    // The list of investment options remains constant (not stored in DB)
    private readonly List<InvestmentOption> _options = new()
    {
        new InvestmentOption { Name = "Short-term", Amount = 10, ExpectedReturn = 20, DurationInSeconds = 10 },
        new InvestmentOption { Name = "Mid-term", Amount = 100, ExpectedReturn = 250, DurationInSeconds = 30 },
        new InvestmentOption { Name = "Long-term", Amount = 1000, ExpectedReturn = 3000, DurationInSeconds = 60 }
    };

    public InvestmentService(
        IDbContextFactory<InvestmentDbContext> contextFactory,
        ILogger<InvestmentService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets a user from the database or creates a new one with starting balance
    /// </summary>
    public async Task<UserAccount> GetOrCreateUserAsync(string username)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var user = await context.Users
            .Include(u => u.ActiveInvestments)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            user = new UserAccount
            {
                Username = username,
                Balance = 500.00m
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Created new user: {Username} with balance ${Balance}", 
                username, user.Balance);
        }

        return user;
    }

    /// <summary>
    /// Returns the list of available investment options
    /// </summary>
    public List<InvestmentOption> GetOptions() => _options;

    /// <summary>
    /// Starts a new investment for a user
    /// </summary>
    public async Task StartInvestmentAsync(string username, string optionName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var option = _options.FirstOrDefault(o => o.Name == optionName)
                     ?? throw new Exception("Investment option not found");

        // Use a transaction to ensure data consistency
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            var user = await context.Users
                .Include(u => u.ActiveInvestments)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Business logic validations
            if (user.Balance < option.Amount)
            {
                throw new Exception("Insufficient balance");
            }

            if (user.ActiveInvestments.Any(i => i.Name == optionName))
            {
                throw new Exception("You already have an active investment in this option");
            }

            // Deduct the amount from balance
            user.Balance -= option.Amount;

            // Create the new investment
            var newInvestment = new ActiveInvestment
            {
                Id = Guid.NewGuid(),
                Username = username,
                Name = option.Name,
                Amount = option.Amount,
                ExpectedReturn = option.ExpectedReturn,
                EndTime = DateTime.UtcNow.AddSeconds(option.DurationInSeconds)
            };

            context.ActiveInvestments.Add(newInvestment);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Investment started: User={Username}, Option={OptionName}, Amount=${Amount}, EndTime={EndTime}",
                username, optionName, option.Amount, newInvestment.EndTime);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets all users with their active investments (used by background service)
    /// </summary>
    public async Task<List<UserAccount>> GetAllUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Users
            .Include(u => u.ActiveInvestments)
            .ToListAsync();
    }

    /// <summary>
    /// Completes an investment and adds the return to user's balance
    /// </summary>
    public async Task CompleteInvestmentAsync(string username, Guid investmentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                _logger.LogWarning("User {Username} not found when completing investment", username);
                return;
            }

            var investment = await context.ActiveInvestments
                .FirstOrDefaultAsync(i => i.Id == investmentId && i.Username == username);

            if (investment == null)
            {
                _logger.LogWarning(
                    "Investment {InvestmentId} not found for user {Username}", 
                    investmentId, username);
                return;
            }

            // Add the expected return to the user's balance
            user.Balance += investment.ExpectedReturn;

            // Remove the investment from active investments
            context.ActiveInvestments.Remove(investment);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Investment completed: User={Username}, Investment={InvestmentName}, Payout=${Payout}, NewBalance=${Balance}",
                username, investment.Name, investment.ExpectedReturn, user.Balance);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing investment {InvestmentId} for user {Username}", 
                investmentId, username);
            throw;
        }
    }
}