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
    public class VerifiableLoggedTest : LoggedTest
    {
        public VerifiableLoggedTest(ITestOutputHelper output) : base(output)
        {
        }

        public IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, testName);

            return new VerifyNoErrorsScope(loggerFactory, disposable, expectedErrorsFilter);
        }

        public IDisposable StartVerifableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, minLogLevel, testName);

            return new VerifyNoErrorsScope(loggerFactory, disposable, expectedErrorsFilter);
        }
    }
}