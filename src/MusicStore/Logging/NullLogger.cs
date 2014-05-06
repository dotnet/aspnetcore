using Microsoft.Framework.Logging;
using System;

namespace MusicStore.Logging
{
    public class NullLogger : ILogger
    {
        public bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            return false;
        }
    }
}