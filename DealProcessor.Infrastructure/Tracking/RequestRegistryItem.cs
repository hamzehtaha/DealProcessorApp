using DealProcessor.Core.Enums;
using DealProcessor.Core.Models;

namespace DealProcessor.Infrastructure.Tracking;

public sealed class RequestRegistryItem
{
    public TradeRequest Request { get; init; } = default!;
    public RequestStatus Status { get; set; } = RequestStatus.Received;
    public TradeExecutionResult? Result { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}