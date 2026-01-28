using Microsoft.AspNetCore.Mvc;
using InvestmentsServer.Models;
using InvestmentsServer.Services;

namespace InvestmentsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentController : ControllerBase
{
    private readonly InvestmentService _service;
    private readonly InvestmentQueue _queue; 
    private readonly ILogger<InvestmentController> _logger;

    public InvestmentController(
        InvestmentService service,
        InvestmentQueue queue, 
        ILogger<InvestmentController> logger)
    {
        _service = service;
        _queue = queue;
        _logger = logger;
    }

   
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

    [HttpGet("options")]
    public IActionResult GetOptions()
    {
        var options = _service.GetOptions();
        return Ok(options);
    }

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

            if (user.Balance < option.Amount)
            {
                return BadRequest(new { message = "Insufficient balance" });
            }

            if (user.ActiveInvestments.Any(i => i.Name == request.OptionName))
            {
                return BadRequest(new { message = "You already have an active investment in this option" });
            }

            await _queue.EnqueueAsync(request);

            _logger.LogInformation(
                "Investment request accepted and queued: User={User}, Option={Option}",
                request.Username,
                request.OptionName);

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