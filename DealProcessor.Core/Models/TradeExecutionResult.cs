using DealProcessor.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class TradeExecutionResult
    {
        public Guid RequestId { get; init; }
        public string ClientId { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public long? MtTicketId { get; init; }
        public string? MtRequestId { get; init; }
        public string? MtRetCode { get; init; }
        public RequestStatus Status { get; init; }
        public DateTime ProcessedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
