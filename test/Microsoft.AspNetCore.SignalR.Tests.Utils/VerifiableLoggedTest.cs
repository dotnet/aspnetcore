// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifiableServerLoggedTest<TStartup> : VerifiableLoggedTest where TStartup : class 
    {
        public ServerFixture<TStartup> ServerFixture { get; }

        public VerifiableServerLoggedTest(ServerFixture<TStartup> serverFixture, ITestOutputHelper output) : base(output)
        {
            ServerFixture = serverFixture;
        }

        public override IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifableLog(out loggerFactory, minLogLevel, testName, expectedErrorsFilter);
            return new ServerLogScope<TStartup>(ServerFixture, loggerFactory, disposable);
        }

        public override IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifableLog(out loggerFactory, testName, expectedErrorsFilter);
            return new ServerLogScope<TStartup>(ServerFixture, loggerFactory, disposable);
        }
    }

    public class ServerLogScope<TStartup> : IDisposable where TStartup : class
    {
        private readonly ServerFixture<TStartup> _serverFixture;
        private readonly IDisposable _wrappedDisposable;
        private readonly ILogger _logger;

        public ServerLogScope(ServerFixture<TStartup> serverFixture, ILoggerFactory loggerFactory, IDisposable wrappedDisposable)
        {
            _serverFixture = serverFixture;
            _wrappedDisposable = wrappedDisposable;
            _logger = loggerFactory.CreateLogger(typeof(ServerLogScope<TStartup>));

            _serverFixture.ServerLogged += ServerFixtureOnServerLogged;
        }

        private void ServerFixtureOnServerLogged(LogRecord logRecord)
        {
            _logger.Log(logRecord.Write.LogLevel, logRecord.Write.EventId, logRecord.Write.State, logRecord.Write.Exception, logRecord.Write.Formatter);
        }

        public void Dispose()
        {
            _serverFixture.ServerLogged -= ServerFixtureOnServerLogged;

            _wrappedDisposable?.Dispose();
        }
    }

    public class VerifiableLoggedTest : LoggedTest
    {
        public VerifiableLoggedTest(ITestOutputHelper output) : base(output)
        {
        }

        public virtual IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, testName);

            return new VerifyNoErrorsScope(loggerFactory, disposable, expectedErrorsFilter);
        }

        public virtual IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, minLogLevel, testName);

            return new VerifyNoErrorsScope(loggerFactory, disposable, expectedErrorsFilter);
        }
    }
}