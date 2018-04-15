// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerLogScope : IDisposable
    {
        private readonly ServerFixture _serverFixture;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDisposable _wrappedDisposable;
        private readonly ConcurrentDictionary<string, ILogger> _loggers;

        public ServerLogScope(ServerFixture serverFixture, ILoggerFactory loggerFactory, IDisposable wrappedDisposable)
        {
            _serverFixture = serverFixture;
            _loggerFactory = loggerFactory;
            _wrappedDisposable = wrappedDisposable;
            _loggers = new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);

            _serverFixture.ServerLogged += ServerFixtureOnServerLogged;
        }

        private void ServerFixtureOnServerLogged(LogRecord logRecord)
        {
            var logger = _loggers.GetOrAdd(logRecord.Write.LoggerName, loggerName => _loggerFactory.CreateLogger(loggerName));
            logger.Log(logRecord.Write.LogLevel, logRecord.Write.EventId, logRecord.Write.State, logRecord.Write.Exception, logRecord.Write.Formatter);
        }

        public void Dispose()
        {
            _serverFixture.ServerLogged -= ServerFixtureOnServerLogged;

            _wrappedDisposable?.Dispose();
        }
    }
}