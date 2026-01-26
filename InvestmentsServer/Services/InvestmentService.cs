using System.Collections.Concurrent;
using InvestmentsServer.Models;

namespace InvestmentsServer.Services;

public class InvestmentService
{
    // Dictionary that stores all users in memory
    private readonly ConcurrentDictionary<string, UserAccount> _users = new();

    // The list of options remains constant
    private readonly List<InvestmentOption> _options = new()
    {
        new InvestmentOption { Name = "Short-term", Amount = 10, ExpectedReturn = 11, DurationInSeconds = 10 },
        new InvestmentOption { Name = "Medium-term", Amount = 100, ExpectedReturn = 120, DurationInSeconds = 60 },
        new InvestmentOption { Name = "Long-term", Amount = 1000, ExpectedReturn = 1500, DurationInSeconds = 300 },
        new InvestmentOption { Name = "Starter Plan", Amount = 100, ExpectedReturn = 110, DurationInSeconds = 300 },
        new InvestmentOption { Name = "Advanced Plan", Amount = 500, ExpectedReturn = 625, DurationInSeconds = 3600 }
    };

    // Function to get or create a user with a starting balance
    public UserAccount GetOrCreateUser(string username)
    {
        return _users.GetOrAdd(username, name => new UserAccount 
        { 
            username = name, 
            balance = 500.00m 
        });
    }

    // This function returns the list for your Market Options table
    public List<InvestmentOption> GetOptions() => _options;

    public void StartInvestment(string username, string optionName)
    {
        var user = GetOrCreateUser(username);
        var option = _options.FirstOrDefault(o => o.Name == optionName)
                     ?? throw new Exception("Option not found");

        lock (user) // lock only the specific user
        {
            if (user.balance < option.Amount) throw new Exception("Insufficient balance");
            if (user.activeInvestments.Any(i => i.Name == optionName)) throw new Exception("Already investing in this track");

            user.balance -= option.Amount;
            user.activeInvestments.Add(new ActiveInvestment
            {
                Id = Guid.NewGuid(),
                Name = option.Name,
                Amount = option.Amount,
                ExpectedReturn = option.ExpectedReturn,
                EndTime = DateTime.UtcNow.AddSeconds(option.DurationInSeconds)
            });
        }
    }

    public IEnumerable<UserAccount> GetAllUsers() => _users.Values;

    public void CompleteInvestment(string username, Guid investmentId)
    {
        var user = GetOrCreateUser(username);
        lock (user)
        {
            var inv = user.activeInvestments.FirstOrDefault(i => i.Id == investmentId);
            if (inv != null)
            {
                user.balance += inv.ExpectedReturn;
                user.activeInvestments.Remove(inv);
            }
        }
    }
}