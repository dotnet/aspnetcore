using System;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestLogger : ILogger
    {
        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return new Disposable(() => { });
        }
    }
}