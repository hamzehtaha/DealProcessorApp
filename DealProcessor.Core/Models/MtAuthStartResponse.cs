using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class MtAuthStartResponse
    {
        public string Retcode { get; set; } = "";
        public string Srv_Rand { get; set; } = "";
    }
}
