using DealProcessor.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class TradeRequest
    {
        public Guid RequestId { get; init; } = Guid.NewGuid();
        public string ClientId { get; init; } = string.Empty;
        public TradeType TradeType { get; init; }
        public string Symbol { get; init; } = string.Empty;
        public decimal Volume { get; init; }
        public decimal? PriceOrder { get; init; }
        public int? Digits { get; init; }
        public decimal? StopLoss { get; init; }
        public decimal? TakeProfit { get; init; }
        public int RetryCount { get; init; } = 0;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
