using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Http
{
    public interface IHttpCounters
    {
        long TotalRequests { get; }
        long CurrentRequests { get; }
        long FailedRequests { get; }

        void RequestStart();

        void RequestStop();

        void RequestException();
    }
}
