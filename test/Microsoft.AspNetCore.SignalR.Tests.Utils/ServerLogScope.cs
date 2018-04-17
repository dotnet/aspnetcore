// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerLogScope : IDisposable
    {
        private readonly ServerFixture _serverFixture;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDisposable _wrappedDisposable;
        private readonly ConcurrentDictionary<string, ILogger> _serverLoggers;
        private readonly ILogger _scopeLogger;

        public ServerLogScope(ServerFixture serverFixture, ILoggerFactory loggerFactory, IDisposable wrappedDisposable)
        {
            _serverFixture = serverFixture;
            _loggerFactory = loggerFactory;
            _wrappedDisposable = wrappedDisposable;
            _scopeLogger = loggerFactory.CreateLogger(typeof(ServerLogScope));
            _serverLoggers = new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);

            _serverFixture.ServerLogged += ServerFixtureOnServerLogged;

            _scopeLogger.LogInformation("Server log scope started.");
        }

        private void ServerFixtureOnServerLogged(LogRecord logRecord)
        {
            // Create (or get) a logger with the same name as the server logger
            var logger = _serverLoggers.GetOrAdd(logRecord.Write.LoggerName, loggerName => _loggerFactory.CreateLogger(loggerName));
            logger.Log(logRecord.Write.LogLevel, logRecord.Write.EventId, logRecord.Write.State, logRecord.Write.Exception, logRecord.Write.Formatter);
        }

        public void Dispose()
        {
            _scopeLogger.LogInformation("Server log scope disposing.");

            _serverFixture.ServerLogged -= ServerFixtureOnServerLogged;

            _wrappedDisposable?.Dispose();
        }
    }
}