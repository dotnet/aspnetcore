using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _requestEnqueued;
        private static readonly Action<ILogger, Exception> _requestDequeued;

        static LoggerExtensions()
        {
            _requestEnqueued = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(1, "Request Enqueued"),
                formatString: "Server is busy; queuing request in middleware."
                );
            _requestDequeued = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(2, "Request Dequeued"),
                formatString: "Space availible on server; request is leaving queue."
                );
        }

        internal static void RequestEnqueued(this ILogger logger)
        {
            _requestEnqueued(logger, null);
        }

        internal static void RequestDequeued(this ILogger logger)
        {
            _requestDequeued(logger, null);
        }
    }
}
