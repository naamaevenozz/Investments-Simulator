using Microsoft.AspNetCore.Mvc;
using InvestmentsServer.Models;
using InvestmentsServer.Services;

namespace InvestmentsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentController : ControllerBase
{
    private readonly InvestmentService _service;
    private readonly ILogger<InvestmentController> _logger;

    public InvestmentController(InvestmentService service, ILogger<InvestmentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns the complete state for a specific user.
    /// If the user doesn't exist, a new account is created with default balance.
    /// </summary>
    [HttpGet("user-data")]
    public IActionResult GetUserData([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        var user = _service.GetOrCreateUser(username);
        return Ok(user);
    }

    /// <summary>
    /// Returns all available investment plans/options.
    /// </summary>
    [HttpGet("options")]
    public IActionResult GetOptions()
    {
        var options = _service.GetOptions();
        return Ok(options);
    }

    /// <summary>
    /// Starts a new investment for a specific user.
    /// </summary>
    [HttpPost("invest")]
    public IActionResult Invest([FromBody] InvestRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.OptionName))
        {
            return BadRequest(new { message = "Username and Option Name are required" });
        }

        try
        {
            _service.StartInvestment(request.Username, request.OptionName);
            _logger.LogInformation("Investment started for user: {User}, Option: {Option}", 
                request.Username, request.OptionName);
            
            return Ok(new { message = "Investment started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Investment failed for user {User}: {Message}", 
                request.Username, ex.Message);
            
            return BadRequest(new { message = ex.Message });
        }
    }
}