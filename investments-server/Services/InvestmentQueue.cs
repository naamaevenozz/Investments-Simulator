using System.Threading.Channels;
using InvestmentsServer.Models;

namespace InvestmentsServer.Services;

/// <summary>
/// In-memory queue for investment requests using Channel<T>
/// This enables asynchronous, event-driven processing
/// </summary>
public class InvestmentQueue
{
    private readonly Channel<InvestRequest> _channel;
    private readonly ILogger<InvestmentQueue> _logger;

    public InvestmentQueue(ILogger<InvestmentQueue> logger)
    {
        _logger = logger;
        
        // Create unbounded channel - can hold unlimited items
        // For production, consider BoundedChannel with capacity limit
        _channel = Channel.CreateUnbounded<InvestRequest>(new UnboundedChannelOptions
        {
            SingleReader = false, // Multiple workers can read
            SingleWriter = false  // Multiple threads can write
        });

        _logger.LogInformation("Investment Queue initialized");
    }

    /// <summary>
    /// Adds an investment request to the queue
    /// </summary>
    public async Task EnqueueAsync(InvestRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
        
        _logger.LogInformation(
            "Investment request queued: User={Username}, Option={OptionName}",
            request.Username,
            request.OptionName);
    }

    /// <summary>
    /// Retrieves the next investment request from the queue
    /// This is a blocking call - waits until an item is available
    /// </summary>
    public async Task<InvestRequest> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var request = await _channel.Reader.ReadAsync(cancellationToken);
        
        _logger.LogDebug(
            "Investment request dequeued: User={Username}, Option={OptionName}",
            request.Username,
            request.OptionName);
        
        return request;
    }

    /// <summary>
    /// Returns the approximate number of items in the queue
    /// Note: This is approximate due to concurrent access
    /// </summary>
    public int GetQueueCount()
    {
        return _channel.Reader.Count;
    }
}