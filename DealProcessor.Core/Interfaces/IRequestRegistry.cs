using DealProcessor.Core.Enums;
using DealProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Interfaces
{
    public interface IRequestRegistry
    {
        bool TryRegister(TradeRequest request);
        bool Exists(Guid requestId);
        void UpdateStatus(Guid requestId, RequestStatus status);
        void Complete(Guid requestId, TradeExecutionResult result);
    }
}
