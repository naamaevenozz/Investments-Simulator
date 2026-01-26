// csharp
using System;

namespace InvestmentsServer.Models;

public class ActiveInvestment : BaseInvestment
{
    /// <summary>
    /// Unique identifier for an active investment.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The UTC time when the investment completes.
    /// </summary>
    public DateTime EndTime { get; set; }
}