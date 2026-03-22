using System.Collections.Concurrent;
using DealProcessor.Core.Enums;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;

namespace DealProcessor.Infrastructure.Tracking;

public sealed class InMemoryRequestRegistry : IRequestRegistry
{
    private readonly ConcurrentDictionary<Guid, RequestRegistryItem> _requests = new();

    public bool TryRegister(TradeRequest request)
    {
        var item = new RequestRegistryItem
        {
            Request = request,
            Status = RequestStatus.Received,
            LastUpdatedUtc = DateTime.UtcNow
        };

        return _requests.TryAdd(request.RequestId, item);
    }

    public bool Exists(Guid requestId)
        => _requests.ContainsKey(requestId);

    public void UpdateStatus(Guid requestId, RequestStatus status)
    {
        if (_requests.TryGetValue(requestId, out var item))
        {
            item.Status = status;
            item.LastUpdatedUtc = DateTime.UtcNow;
        }
    }

    public void Complete(Guid requestId, TradeExecutionResult result)
    {
        if (_requests.TryGetValue(requestId, out var item))
        {
            item.Result = result;
            item.Status = result.Status;
            item.LastUpdatedUtc = DateTime.UtcNow;
        }
    }
}