// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public abstract class LoggedTest
    {
        // Obsolete but keeping for back compat
        public LoggedTest(ITestOutputHelper output = null)
        {
            TestOutputHelper = output;
        }

        // Internal for testing
        internal string ResolvedTestMethodName { get; set; }

        // Internal for testing
        internal string ResolvedTestClassName { get; set; }

        public ILogger Logger { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public ITestOutputHelper TestOutputHelper { get; set; }

        public ITestSink TestSink { get; set; }

        public void AddTestLogging(IServiceCollection services) => services.AddSingleton(LoggerFactory);

        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) => StartLog(out loggerFactory, LogLevel.Information, testName);

        public IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return AssemblyTestLog.ForAssembly(GetType().GetTypeInfo().Assembly).StartTestLog(TestOutputHelper, GetType().FullName, out loggerFactory, minLogLevel, testName);
        }

        public virtual void AdditionalSetup() { }
    }
}
