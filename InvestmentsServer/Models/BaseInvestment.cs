// csharp
namespace InvestmentsServer.Models;

public abstract class BaseInvestment
{
    /// <summary>
    /// The display name of the investment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The amount of currency invested.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The expected payout amount when the investment completes.
    /// </summary>
    public decimal ExpectedReturn { get; set; }
}