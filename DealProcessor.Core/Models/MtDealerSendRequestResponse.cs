using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class MtDealerSendRequestResponse
    {
        public string Retcode { get; set; } = string.Empty;
        public MtDealerSendRequestAnswer? Answer { get; set; }
    }

    public sealed class MtDealerSendRequestAnswer
    {
        public string ID { get; set; } = string.Empty;
    }
}
