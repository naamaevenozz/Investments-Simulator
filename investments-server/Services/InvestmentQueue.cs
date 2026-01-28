using System.Threading.Channels;
using InvestmentsServer.Models;

namespace InvestmentsServer.Services;

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

    public async Task EnqueueAsync(InvestRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
        
        _logger.LogInformation(
            "Investment request queued: User={Username}, Option={OptionName}",
            request.Username,
            request.OptionName);
    }

 
    public async Task<InvestRequest> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var request = await _channel.Reader.ReadAsync(cancellationToken);
        
        _logger.LogDebug(
            "Investment request dequeued: User={Username}, Option={OptionName}",
            request.Username,
            request.OptionName);
        
        return request;
    }


    public int GetQueueCount()
    {
        return _channel.Reader.Count;
    }
}