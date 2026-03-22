using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class MtDealerTradeRequestDto
    {
        public string Action { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string TypeFill { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PriceOrder { get; set; } = string.Empty;
        public string Digits { get; set; } = string.Empty;
        public string? SL { get; set; }
        public string? TP { get; set; }
    }
}
