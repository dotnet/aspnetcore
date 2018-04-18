// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifiableServerLoggedTest : VerifiableLoggedTest
    {
        public ServerFixture ServerFixture { get; }

        public VerifiableServerLoggedTest(ServerFixture serverFixture, ITestOutputHelper output) : base(output)
        {
            ServerFixture = serverFixture;
        }

        public override IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifableLog(out loggerFactory, minLogLevel, testName, expectedErrorsFilter);
            return new ServerLogScope(ServerFixture, loggerFactory, disposable);
        }

        public override IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifableLog(out loggerFactory, testName, expectedErrorsFilter);
            return new ServerLogScope(ServerFixture, loggerFactory, disposable);
        }
    }
}