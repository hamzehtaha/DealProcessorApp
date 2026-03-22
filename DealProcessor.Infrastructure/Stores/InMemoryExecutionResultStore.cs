using System.Collections.Concurrent;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;

namespace DealProcessor.Infrastructure.Stores;

public sealed class InMemoryExecutionResultStore : IExecutionResultStore
{
    private readonly ConcurrentDictionary<Guid, TradeExecutionResult> _results = new();

    public void Save(TradeExecutionResult result)
    {
        _results[result.RequestId] = result;
    }

    public bool TryGet(Guid requestId, out TradeExecutionResult? result)
    {
        var found = _results.TryGetValue(requestId, out var temp);
        result = temp;
        return found;
    }

    public IReadOnlyCollection<TradeExecutionResult> GetAll()
        => _results.Values.ToList().AsReadOnly();
}