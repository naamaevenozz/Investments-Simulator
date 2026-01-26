namespace InvestmentsServer.Models;

/// <summary>
/// Data Transfer Object for investment requests from the frontend.
/// </summary>
public class InvestRequest
{
    /// <summary>
    /// The name of the investment option the user wants to start.
    /// </summary>
    public string OptionName { get; set; } = string.Empty;
	  /// <summary>
	 /// The username of the user making the investment.
    /// </summary>
	public string Username { get; set; } = string.Empty;
}