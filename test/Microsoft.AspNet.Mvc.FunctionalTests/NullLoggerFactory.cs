using Microsoft.Framework.Logging;
using System;

namespace Microsoft.AspNet.Mvc
{
    public class NullLoggerFactory : ILoggerFactory
    {
        public static NullLoggerFactory Instance = new NullLoggerFactory();

        public ILogger Create(string name)
        {
            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {

        }
    }

    public class NullLogger : ILogger
    {
        public static NullLogger Instance = new NullLogger();

        public IDisposable BeginScope(object state)
        {
            return NullDisposable.Instance;
        }

        public void Write(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
        }

        public bool IsEnabled(TraceType eventType)
        {
            return false;
        }
    }

    public class NullDisposable : IDisposable
    {
        public static NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
            // intentionally does nothing
        }
    }
}