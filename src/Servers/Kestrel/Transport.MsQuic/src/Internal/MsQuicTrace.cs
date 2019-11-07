// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicTrace : IMsQuicTrace
    {
        private static readonly Action<ILogger, string, Exception> _acceptedConnection =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, nameof(NewConnection)), @"Connection id ""{ConnectionId}"" accepted.");
        private static readonly Action<ILogger, string, Exception> _acceptedStream =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, nameof(NewStream)), @"Stream id ""{ConnectionId}"" accepted.");
        private static readonly Action<ILogger, string, string, Exception> _connectionError =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(6, nameof(NewStream)), @"Connection id ""{ConnectionId}"" hit an exception: ""{Reason}"".");
        private static readonly Action<ILogger, string, string, Exception> _streamError =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(7, nameof(NewStream)), @"Connection id ""{ConnectionId}"" hit an exception: ""{Reason}"".");

        private ILogger _logger;

        public MsQuicTrace(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        public void NewConnection(string connectionId)
        {
            _acceptedConnection(_logger, connectionId, null);
        }

        public void NewStream(string streamId)
        {
            _acceptedStream(_logger, streamId, null);
        }
        public void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex.Message, ex);
        }

        public void StreamError(string streamId, Exception ex)
        {
            _streamError(_logger, streamId, ex.Message, ex);
        }
    }
}
