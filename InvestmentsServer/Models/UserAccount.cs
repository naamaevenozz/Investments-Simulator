namespace InvestmentsServer.Models;

/// <summary>
/// Represents a user's investment account with balance and active investments
/// </summary>
public class UserAccount
{
    /// <summary>
    /// User's name (username or first name)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Current account balance
    /// </summary>
    public decimal Balance { get; set; } = 1000m; 
    
    /// <summary>
    /// List of currently active investments
    /// </summary>
    public List<ActiveInvestment> ActiveInvestments { get; set; } = new();
}