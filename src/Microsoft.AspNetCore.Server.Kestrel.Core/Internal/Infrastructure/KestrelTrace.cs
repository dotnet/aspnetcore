// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class KestrelTrace : IKestrelTrace
    {
        private static readonly Action<ILogger, string, Exception> _connectionStart =
            LoggerMessage.Define<string>(LogLevel.Debug, 1, @"Connection id ""{ConnectionId}"" started.");

        private static readonly Action<ILogger, string, Exception> _connectionStop =
            LoggerMessage.Define<string>(LogLevel.Debug, 2, @"Connection id ""{ConnectionId}"" stopped.");

        private static readonly Action<ILogger, string, Exception> _connectionPause =
            LoggerMessage.Define<string>(LogLevel.Debug, 4, @"Connection id ""{ConnectionId}"" paused.");

        private static readonly Action<ILogger, string, Exception> _connectionResume =
            LoggerMessage.Define<string>(LogLevel.Debug, 5, @"Connection id ""{ConnectionId}"" resumed.");

        private static readonly Action<ILogger, string, Exception> _connectionKeepAlive =
            LoggerMessage.Define<string>(LogLevel.Debug, 9, @"Connection id ""{ConnectionId}"" completed keep alive response.");

        private static readonly Action<ILogger, string, Exception> _connectionDisconnect =
            LoggerMessage.Define<string>(LogLevel.Debug, 10, @"Connection id ""{ConnectionId}"" disconnecting.");

        private static readonly Action<ILogger, string, string, Exception> _applicationError =
            LoggerMessage.Define<string, string>(LogLevel.Error, 13, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": An unhandled exception was thrown by the application.");

        private static readonly Action<ILogger, Exception> _notAllConnectionsClosedGracefully =
            LoggerMessage.Define(LogLevel.Debug, 16, "Some connections failed to close gracefully during server shutdown.");

        private static readonly Action<ILogger, string, string, Exception> _connectionBadRequest =
            LoggerMessage.Define<string, string>(LogLevel.Information, 17, @"Connection id ""{ConnectionId}"" bad request data: ""{message}""");

        private static readonly Action<ILogger, string, long, Exception> _connectionHeadResponseBodyWrite =
            LoggerMessage.Define<string, long>(LogLevel.Debug, 18, @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.");

        private static readonly Action<ILogger, string, Exception> _requestProcessingError =
            LoggerMessage.Define<string>(LogLevel.Information, 20, @"Connection id ""{ConnectionId}"" request processing ended abnormally.");

        private static readonly Action<ILogger, Exception> _notAllConnectionsAborted =
            LoggerMessage.Define(LogLevel.Debug, 21, "Some connections failed to abort during server shutdown.");

        private static readonly Action<ILogger, TimeSpan, DateTimeOffset, Exception> _heartbeatSlow =
            LoggerMessage.Define<TimeSpan, DateTimeOffset>(LogLevel.Warning, 22, @"Heartbeat took longer than ""{interval}"" at ""{now}"".");

        private static readonly Action<ILogger, string, Exception> _applicationNeverCompleted =
            LoggerMessage.Define<string>(LogLevel.Critical, 23, @"Connection id ""{ConnectionId}"" application never completed");

        private static readonly Action<ILogger, string, Exception> _connectionRejected =
            LoggerMessage.Define<string>(LogLevel.Warning, 24, @"Connection id ""{ConnectionId}"" rejected because the maximum number of concurrent connections has been reached.");

        private static readonly Action<ILogger, string, string, Exception> _requestBodyStart =
            LoggerMessage.Define<string, string>(LogLevel.Debug, 25, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": started reading request body.");

        private static readonly Action<ILogger, string, string, Exception> _requestBodyDone =
            LoggerMessage.Define<string, string>(LogLevel.Debug, 26, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": done reading request body.");

        private static readonly Action<ILogger, string, string, double, Exception> _requestBodyMinimumDataRateNotSatisfied =
            LoggerMessage.Define<string, string, double>(LogLevel.Information, 27, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the request timed out because it was not sent by the client at a minimum of {Rate} bytes/second.");

        private static readonly Action<ILogger, string, string, Exception> _responseMinimumDataRateNotSatisfied =
            LoggerMessage.Define<string, string>(LogLevel.Information, 28, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the connection was closed becuase the response was not read by the client at the specified minimum data rate.");

        protected readonly ILogger _logger;

        public KestrelTrace(ILogger logger)
        {
            _logger = logger;
        }

        public virtual void ConnectionStart(string connectionId)
        {
            _connectionStart(_logger, connectionId, null);
        }

        public virtual void ConnectionStop(string connectionId)
        {
            _connectionStop(_logger, connectionId, null);
        }

        public virtual void ConnectionPause(string connectionId)
        {
            _connectionPause(_logger, connectionId, null);
        }

        public virtual void ConnectionResume(string connectionId)
        {
            _connectionResume(_logger, connectionId, null);
        }

        public virtual void ConnectionKeepAlive(string connectionId)
        {
            _connectionKeepAlive(_logger, connectionId, null);
        }

        public void ConnectionRejected(string connectionId)
        {
            _connectionRejected(_logger, connectionId, null);
        }

        public virtual void ConnectionDisconnect(string connectionId)
        {
            _connectionDisconnect(_logger, connectionId, null);
        }

        public virtual void ApplicationError(string connectionId, string traceIdentifier, Exception ex)
        {
            _applicationError(_logger, connectionId, traceIdentifier, ex);
        }

        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count)
        {
            _connectionHeadResponseBodyWrite(_logger, connectionId, count, null);
        }

        public void NotAllConnectionsClosedGracefully()
        {
            _notAllConnectionsClosedGracefully(_logger, null);
        }

        public void ConnectionBadRequest(string connectionId, BadHttpRequestException ex)
        {
            _connectionBadRequest(_logger, connectionId, ex.Message, ex);
        }

        public virtual void RequestProcessingError(string connectionId, Exception ex)
        {
            _requestProcessingError(_logger, connectionId, ex);
        }

        public void NotAllConnectionsAborted()
        {
            _notAllConnectionsAborted(_logger, null);
        }

        public virtual void HeartbeatSlow(TimeSpan interval, DateTimeOffset now)
        {
            _heartbeatSlow(_logger, interval, now, null);
        }

        public virtual void ApplicationNeverCompleted(string connectionId)
        {
            _applicationNeverCompleted(_logger, connectionId, null);
        }

        public virtual void RequestBodyStart(string connectionId, string traceIdentifier)
        {
            _requestBodyStart(_logger, connectionId, traceIdentifier, null);
        }

        public virtual void RequestBodyDone(string connectionId, string traceIdentifier)
        {
            _requestBodyDone(_logger, connectionId, traceIdentifier, null);
        }

        public void RequestBodyMininumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate)
        {
            _requestBodyMinimumDataRateNotSatisfied(_logger, connectionId, traceIdentifier, rate, null);
        }

        public void ResponseMininumDataRateNotSatisfied(string connectionId, string traceIdentifier)
        {
            _responseMinimumDataRateNotSatisfied(_logger, connectionId, traceIdentifier, null);
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        public virtual bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public virtual IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
    }
}
