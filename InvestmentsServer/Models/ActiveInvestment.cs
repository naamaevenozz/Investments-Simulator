using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentsServer.Models;

/// <summary>
/// Represents an active investment with all its details
/// </summary>
[Table("ActiveInvestments")]
public class ActiveInvestment : BaseInvestment
{
    /// <summary>
    /// Unique identifier for an active investment - Primary Key
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the User who owns this investment
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The UTC time when the investment completes
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }+
}