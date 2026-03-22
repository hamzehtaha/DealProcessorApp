using System.Threading.Channels;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;

namespace DealProcessor.Infrastructure.Queues;

public sealed class TradeRequestQueue : ITradeRequestQueue
{
    private readonly Channel<TradeRequest> _channel;
    private int _count;

    public TradeRequestQueue()
    {
        _channel = Channel.CreateUnbounded<TradeRequest>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public int Count => Volatile.Read(ref _count);

    public async ValueTask EnqueueAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _count);

        try
        {
            await _channel.Writer.WriteAsync(request, cancellationToken);
        }
        catch
        {
            Interlocked.Decrement(ref _count);
            throw;
        }
    }

    public async ValueTask<TradeRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        var request = await _channel.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref _count);
        return request;
    }
}