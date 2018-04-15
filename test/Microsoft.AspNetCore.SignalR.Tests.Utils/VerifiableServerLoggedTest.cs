// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
}