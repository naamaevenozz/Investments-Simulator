namespace DefaultNamespace;

public abstract class BaseInvestment
{
    public string Name { get; set; } = string.Empty; [cite: 20, 21]
    public decimal Amount { get; set; } // File: `Models/BaseInvestment.cs`
    namespace DefaultNamespace;
    
    /// <summary>
    /// Represents the common properties of an investment option or an active investment.
    /// </summary>
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
    public decimal ExpectedReturn { get; set; } [cite: 20, 21]
}