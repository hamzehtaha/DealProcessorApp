using DealProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Interfaces
{
    public interface IMtTradeGateway
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
        Task<ValidationResult> ValidateAsync(TradeRequest request, CancellationToken cancellationToken);
        Task<TradeExecutionResult> ExecuteAsync(TradeRequest request, CancellationToken cancellationToken);
    }
}
