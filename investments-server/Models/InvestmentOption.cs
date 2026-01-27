namespace InvestmentsServer.Models;

public class InvestmentOption : BaseInvestment
{
    /// <summary>
    /// Duration in seconds for the investment to complete.
    /// </summary>
    public int DurationInSeconds { get; set; }
}