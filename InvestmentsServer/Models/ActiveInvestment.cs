namespace InvestmentsServer.Models;

public class ActiveInvestment
{
    public Guid Id { get; set; } = Guid.NewGuid(); [cite: 20, 39]
    public DateTime EndTime { get; set; } // File: `Models/ActiveInvestment.cs`
    using DefaultNamespace;
    
    namespace InvestmentsServer.Models;
    
    /// <summary>
    /// Represents an active investment started by the user.
    /// Inherits <see cref="BaseInvestment"/> to include Name, Amount and ExpectedReturn.
    /// </summary>
    public class ActiveInvestment : BaseInvestment
    {
        /// <summary>
        /// Unique identifier for this active investment instance.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
    
        /// <summary>
        /// The UTC time when the investment will complete and payout is available.
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}