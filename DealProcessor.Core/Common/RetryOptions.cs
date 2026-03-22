using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Common
{
    public sealed class RetryOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 2;
        public int DelayMs { get; set; } = 500;
    }
}
