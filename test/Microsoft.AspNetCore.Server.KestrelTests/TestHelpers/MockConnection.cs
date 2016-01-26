using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockConnection : Connection
    {
        public MockConnection(UvStreamHandle socket)
            : base (new ListenerContext(), socket)
        {

        }

        public override void Abort()
        {
            if (RequestAbortedSource != null)
            {
                RequestAbortedSource.Cancel();
            }
        }

        public CancellationTokenSource RequestAbortedSource { get; set; }
    }
}
