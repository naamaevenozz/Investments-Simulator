namespace InvestmentsServer.Models;

/// <summary>
/// Represents a user's investment account with balance and active investments
/// </summary>
public class UserAccount
{
    /// <summary>
    /// User's name (username or first name)
    /// </summary>
    public string username { get; set; } = string.Empty;
    
    /// <summary>
    /// Current account balance
    /// </summary>
    public decimal balance { get; set; } = 500m; 
    
    /// <summary>
    /// List of currently active investments
    /// </summary>
    public List<ActiveInvestment> activeInvestments { get; set; } = new();
}