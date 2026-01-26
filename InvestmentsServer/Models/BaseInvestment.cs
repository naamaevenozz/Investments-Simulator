using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentsServer.Models;

/// <summary>
/// Base class for investment-related entities
/// </summary>
public abstract class BaseInvestment
{
    /// <summary>
    /// The display name of the investment
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The amount of currency invested
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// The expected payout amount when the investment completes
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedReturn { get; set; }
}