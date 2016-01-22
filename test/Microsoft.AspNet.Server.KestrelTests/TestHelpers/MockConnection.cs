using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.KestrelTests.TestHelpers
{
    public class MockConnection : Connection
    {
        public MockConnection(UvStreamHandle socket)
            : base (new ListenerContext(), socket)
        {

        }

        public override void Abort()
        {
        }
    }
}
