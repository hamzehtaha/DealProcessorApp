using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Common
{
    public sealed class Mt5Options
    {
        public bool UseRealMt5Api { get; set; } = false;
        public string BaseUrl { get; set; } = string.Empty;
        public ulong ManagerLogin { get; set; }
        public string Password { get; set; } = string.Empty;
        public ulong CentralTradeLogin { get; set; }
        public int PollIntervalMs { get; set; } = 500;
        public int PollTimeoutMs { get; set; } = 10000;
    }
}
