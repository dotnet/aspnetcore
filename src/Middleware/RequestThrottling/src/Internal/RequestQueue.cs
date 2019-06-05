using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    interface RequestQueue : IDisposable
    {
        public int TotalRequests { get; }

        public Task<bool> TryEnterQueueAsync();

        public void Release();
    }
}
