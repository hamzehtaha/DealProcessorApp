using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Enums
{
    public enum RequestStatus
    {
        Received = 1,
        Queued = 2,
        Validating = 3,
        Rejected = 4,
        Executing = 5,
        Succeeded = 6,
        Failed = 7,
        Duplicate = 8
    }
}
