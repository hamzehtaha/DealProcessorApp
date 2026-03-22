using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Common
{
    public sealed class SimulationOptions
    {
        public int ClientCount { get; set; } = 5;
        public int RequestsPerClient { get; set; } = 1000;
        public int ShutdownDelayMs { get; set; } = 10000;
    }
}
