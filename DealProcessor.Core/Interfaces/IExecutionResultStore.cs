using DealProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Interfaces
{
    public interface IExecutionResultStore
    {
        void Save(TradeExecutionResult result);
        bool TryGet(Guid requestId, out TradeExecutionResult? result);
        IReadOnlyCollection<TradeExecutionResult> GetAll();
    }
}
