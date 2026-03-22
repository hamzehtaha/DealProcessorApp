using DealProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Interfaces
{
    public interface ITradeRequestQueue
    {
        ValueTask EnqueueAsync(TradeRequest request, CancellationToken cancellationToken);
        ValueTask<TradeRequest> DequeueAsync(CancellationToken cancellationToken);
        int Count { get; }
    }
}
