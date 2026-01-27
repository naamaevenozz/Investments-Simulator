using Microsoft.AspNetCore.Mvc;
using InvestmentsServer.Models;
using InvestmentsServer.Services;

namespace InvestmentsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentController : ControllerBase
{
    private readonly InvestmentService _service;
    private readonly InvestmentQueue _queue; // NEW: Queue for event-driven processing
    private readonly ILogger<InvestmentController> _logger;

    public InvestmentController(
        InvestmentService service,
        InvestmentQueue queue, // NEW: Inject the queue
        ILogger<InvestmentController> logger)
    {
        _service = service;
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Returns the complete state for a specific user.
    /// If the user doesn't exist, a new account is created with default balance.
    /// </summary>
    [HttpGet("user-data")]
    public async Task<IActionResult> GetUserData([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        try
        {
            var user = await _service.GetOrCreateUserAsync(username);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user data for {Username}", username);
            return StatusCode(500, new { message = "An error occurred while retrieving user data" });
        }
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
    /// EVENT-DRIVEN: Queues an investment request for asynchronous processing
    /// Returns immediately with 202 Accepted (does NOT wait for processing)
    /// </summary>
    [HttpPost("invest")]
    public async Task<IActionResult> Invest([FromBody] InvestRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.OptionName))
        {
            return BadRequest(new { message = "Username and Option Name are required" });
        }

        try
        {
            // IMPORTANT: Pre-validation BEFORE adding to queue
            // This ensures we don't queue invalid requests
            var user = await _service.GetOrCreateUserAsync(request.Username);
            var option = _service.GetOptions()
                .FirstOrDefault(o => o.Name == request.OptionName);

            if (option == null)
            {
                return BadRequest(new { message = "Investment option not found" });
            }

            // Check balance
            if (user.Balance < option.Amount)
            {
                return BadRequest(new { message = "Insufficient balance" });
            }

            // Check for duplicate active investment
            if (user.ActiveInvestments.Any(i => i.Name == request.OptionName))
            {
                return BadRequest(new { message = "You already have an active investment in this option" });
            }

            // EVENT-DRIVEN: Add to queue and return IMMEDIATELY
            await _queue.EnqueueAsync(request);

            _logger.LogInformation(
                "Investment request accepted and queued: User={User}, Option={Option}",
                request.Username,
                request.OptionName);

            // Return 202 Accepted - indicates request is being processed asynchronously
            return Accepted(new 
            { 
                message = "Investment request received and is being processed",
                status = "queued"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error queueing investment request for user {User}",
                request.Username);

            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// NEW: Returns queue statistics (optional - for monitoring)
    /// </summary>
    [HttpGet("queue-status")]
    public IActionResult GetQueueStatus()
    {
        return Ok(new
        {
            queueLength = _queue.GetQueueCount(),
            status = "operational"
        });
    }
}