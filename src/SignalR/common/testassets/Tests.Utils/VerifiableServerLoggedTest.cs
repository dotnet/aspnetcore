// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifiableServerLoggedTest : VerifiableLoggedTest
    {
        private readonly Func<WriteContext, bool> _globalExpectedErrorsFilter;

        public ServerFixture ServerFixture { get; }

        public VerifiableServerLoggedTest(ServerFixture serverFixture, ITestOutputHelper output) : base(output)
        {
            ServerFixture = serverFixture;

            _globalExpectedErrorsFilter = (writeContext) =>
            {
                // Suppress https://github.com/aspnet/SignalR/issues/2034
                if (writeContext.LoggerName == "Microsoft.AspNetCore.Http.Connections.Client.Internal.ServerSentEventsTransport" &&
                    writeContext.Message.StartsWith("Error while sending to") &&
                    writeContext.Exception is HttpRequestException)
                {
                    return true;
                }

                return false;
            };
        }

        private Func<WriteContext, bool> ResolveExpectedErrorsFilter(Func<WriteContext, bool> expectedErrorsFilter)
        {
            if (expectedErrorsFilter == null)
            {
                return _globalExpectedErrorsFilter;
            }

            return (writeContext) =>
            {
                if (expectedErrorsFilter(writeContext))
                {
                    return true;
                }

                return _globalExpectedErrorsFilter(writeContext);
            };
        }

        public override IDisposable StartVerifiableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifiableLog(out loggerFactory, minLogLevel, testName, ResolveExpectedErrorsFilter(expectedErrorsFilter));
            return new ServerLogScope(ServerFixture, loggerFactory, disposable);
        }

        public override IDisposable StartVerifiableLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = base.StartVerifiableLog(out loggerFactory, testName, ResolveExpectedErrorsFilter(expectedErrorsFilter));
            return new ServerLogScope(ServerFixture, loggerFactory, disposable);
        }
    }
}