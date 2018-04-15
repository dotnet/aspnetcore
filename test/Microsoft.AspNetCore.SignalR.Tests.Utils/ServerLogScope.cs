// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ServerLogScope : IDisposable
    {
        private readonly ServerFixture _serverFixture;
        private readonly IDisposable _wrappedDisposable;
        //private readonly ILogger _logger;

        public ServerLogScope(ServerFixture serverFixture, ILoggerFactory loggerFactory, IDisposable wrappedDisposable)
        {
            _serverFixture = serverFixture;
            _wrappedDisposable = wrappedDisposable;
            //_logger = loggerFactory.CreateLogger(typeof(ServerLogScope<TStartup>));

            _serverFixture.ServerLogged += ServerFixtureOnServerLogged;
        }

        private void ServerFixtureOnServerLogged(LogRecord logRecord)
        {
            //_logger.Log(logRecord.Write.LogLevel, logRecord.Write.EventId, logRecord.Write.State, logRecord.Write.Exception, logRecord.Write.Formatter);
        }

        public void Dispose()
        {
            _serverFixture.ServerLogged -= ServerFixtureOnServerLogged;

            _wrappedDisposable?.Dispose();
        }
    }
}