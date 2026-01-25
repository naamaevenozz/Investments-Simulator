namespace InvestmentsServer.Models;

public class InvestmentOption : BaseInvestment
{ 
    // File: `Models/InvestmentOption.cs`
    using DefaultNamespace;
    
    namespace InvestmentsServer.Models;
    
    /// <summary>
    /// Defines an investment option available for the user to start.
    /// Inherits shared properties from <see cref="BaseInvestment"/>.
    /// </summary>
    public class InvestmentOption : BaseInvestment
    {
        /// <summary>
        /// Duration in seconds until the investment completes.
        /// </summary>
        public int DurationInSeconds { get; set; }
    }public int DurationInSeconds { get; set; } 
}