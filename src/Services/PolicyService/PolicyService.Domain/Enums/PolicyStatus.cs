using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyService.Domain.Enums
{
    public enum PolicyStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }
}
