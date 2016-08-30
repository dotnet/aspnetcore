using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class TestKestrelTrace : KestrelTrace
    {
        public TestKestrelTrace() : this(new TestApplicationErrorLogger())
        {
        }

        public TestKestrelTrace(ILogger testLogger) : base(testLogger)
        {
        }

        public override void ConnectionRead(string connectionId, int count)
        {
            //_logger.LogDebug(1, @"Connection id ""{ConnectionId}"" recv {count} bytes.", connectionId, count);
        }

        public override void ConnectionWrite(string connectionId, int count)
        {
            //_logger.LogDebug(1, @"Connection id ""{ConnectionId}"" send {count} bytes.", connectionId, count);
        }

        public override void ConnectionWriteCallback(string connectionId, int status)
        {
            //_logger.LogDebug(1, @"Connection id ""{ConnectionId}"" send finished with status {status}.", connectionId, status);
        }
    }
}