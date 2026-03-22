using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class MtTradeValidationInfo
    {
        public bool SymbolExists { get; init; }
        public bool VolumeValid { get; init; }
        public bool MarginSufficient { get; init; }
        public string? Details { get; init; }
    }
}
