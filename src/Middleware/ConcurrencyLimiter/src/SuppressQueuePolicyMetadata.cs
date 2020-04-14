using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal class SuppressQueuePolicyMetadata : ISuppressQueuePolicyMetadata
    {
        public static ISuppressQueuePolicyMetadata Default = new SuppressQueuePolicyMetadata();
    }
}
