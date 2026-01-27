using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentsServer.Models;

/// <summary>
/// Represents a user's investment account with balance and active investments
/// </summary>
[Table("Users")]
public class UserAccount
{
    /// <summary>
    /// User's name (username or first name) - Primary Key
    /// </summary>
    [Key]
    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Current account balance
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 500m; 
    
    /// <summary>
    /// List of currently active investments (Navigation property)
    /// </summary>
    public List<ActiveInvestment> ActiveInvestments { get; set; } = new();
}