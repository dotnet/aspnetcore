using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class DefaultHttpCounters : IHttpCounters
    {
        private long _totalRequests;
        private long _currentRequests;
        private long _failedRequests;

        public long TotalRequests => _totalRequests;

        public long CurrentRequests => _currentRequests;

        public long FailedRequests => _failedRequests;

        public void RequestException()
        {
            Interlocked.Increment(ref _failedRequests);
        }

        public void RequestStart()
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _currentRequests);
        }

        public void RequestStop()
        {
            Interlocked.Decrement(ref _currentRequests);
        }
    }
}
