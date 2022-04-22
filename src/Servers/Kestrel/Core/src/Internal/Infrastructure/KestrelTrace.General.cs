// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void ApplicationError(string connectionId, string traceIdentifier, Exception ex)
    {
        GeneralLog.ApplicationError(_generalLogger, connectionId, traceIdentifier, ex);
    }

    public void ConnectionHeadResponseBodyWrite(string connectionId, long count)
    {
        GeneralLog.ConnectionHeadResponseBodyWrite(_generalLogger, connectionId, count);
    }

    public void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now)
    {
        // while the heartbeat does loop over connections, this log is usually an indicator of threadpool starvation
        GeneralLog.HeartbeatSlow(_generalLogger, now, heartbeatDuration, interval);
    }

    public void ApplicationNeverCompleted(string connectionId)
    {
        GeneralLog.ApplicationNeverCompleted(_generalLogger, connectionId);
    }

    public void RequestBodyStart(string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyStart(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyDone(string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyDone(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyNotEntirelyRead(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyDrainTimedOut(_generalLogger, connectionId, traceIdentifier);
    }

    public void InvalidResponseHeaderRemoved()
    {
        GeneralLog.InvalidResponseHeaderRemoved(_generalLogger);
    }

    private static partial class GeneralLog
    {
        [LoggerMessage(13, LogLevel.Error, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": An unhandled exception was thrown by the application.", EventName = "ApplicationError")]
        public static partial void ApplicationError(ILogger logger, string connectionId, string traceIdentifier, Exception ex);

        [LoggerMessage(18, LogLevel.Debug, @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.", EventName = "ConnectionHeadResponseBodyWrite")]
        public static partial void ConnectionHeadResponseBodyWrite(ILogger logger, string connectionId, long count);

        [LoggerMessage(22, LogLevel.Warning, @"As of ""{now}"", the heartbeat has been running for ""{heartbeatDuration}"" which is longer than ""{interval}"". This could be caused by thread pool starvation.", EventName = "HeartbeatSlow")]
        public static partial void HeartbeatSlow(ILogger logger, DateTimeOffset now, TimeSpan heartbeatDuration, TimeSpan interval);

        [LoggerMessage(23, LogLevel.Critical, @"Connection id ""{ConnectionId}"" application never completed.", EventName = "ApplicationNeverCompleted")]
        public static partial void ApplicationNeverCompleted(ILogger logger, string connectionId);

        [LoggerMessage(25, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": started reading request body.", EventName = "RequestBodyStart", SkipEnabledCheck = true)]
        public static partial void RequestBodyStart(ILogger logger, string connectionId, string traceIdentifier);

        [LoggerMessage(26, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": done reading request body.", EventName = "RequestBodyDone", SkipEnabledCheck = true)]
        public static partial void RequestBodyDone(ILogger logger, string connectionId, string traceIdentifier);

        [LoggerMessage(32, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application completed without reading the entire request body.", EventName = "RequestBodyNotEntirelyRead")]
        public static partial void RequestBodyNotEntirelyRead(ILogger logger, string connectionId, string traceIdentifier);

        [LoggerMessage(33, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": automatic draining of the request body timed out after taking over 5 seconds.", EventName = "RequestBodyDrainTimedOut")]
        public static partial void RequestBodyDrainTimedOut(ILogger logger, string connectionId, string traceIdentifier);

        [LoggerMessage(41, LogLevel.Warning, "One or more of the following response headers have been removed because they are invalid for HTTP/2 and HTTP/3 responses: 'Connection', 'Transfer-Encoding', 'Keep-Alive', 'Upgrade' and 'Proxy-Connection'.", EventName = "InvalidResponseHeaderRemoved")]
        public static partial void InvalidResponseHeaderRemoved(ILogger logger);

        // Highest shared ID is 63. New consecutive IDs start at 64
    }
}
