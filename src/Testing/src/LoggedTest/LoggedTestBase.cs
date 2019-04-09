// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestBase : ILoggedTest
    {
        private ExceptionDispatchInfo _initializationException;

        private IDisposable _testLog;

        // Obsolete but keeping for back compat
        public LoggedTestBase(ITestOutputHelper output = null)
        {
            TestOutputHelper = output;
        }

        // Internal for testing
        internal string ResolvedTestClassName { get; set; }

        internal RepeatContext RepeatContext { get; set; }

        public string ResolvedLogOutputDirectory { get; set; }

        public string ResolvedTestMethodName { get; set; }

        public ILogger Logger { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public ITestOutputHelper TestOutputHelper { get; set; }

        public void AddTestLogging(IServiceCollection services) => services.AddSingleton(LoggerFactory);

        // For back compat
        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) => StartLog(out loggerFactory, LogLevel.Debug, testName);

        // For back compat
        public IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return AssemblyTestLog.ForAssembly(GetType().GetTypeInfo().Assembly).StartTestLog(TestOutputHelper, GetType().FullName, out loggerFactory, minLogLevel, testName);
        }

        public virtual void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            try
            {
                TestOutputHelper = testOutputHelper;

                var classType = GetType();
                var logLevelAttribute = methodInfo.GetCustomAttribute<LogLevelAttribute>()
                                        ?? methodInfo.DeclaringType.GetCustomAttribute<LogLevelAttribute>()
                                        ?? methodInfo.DeclaringType.Assembly.GetCustomAttribute<LogLevelAttribute>();
                var testName = testMethodArguments.Aggregate(methodInfo.Name, (a, b) => $"{a}-{(b ?? "null")}");

                var useShortClassName = methodInfo.DeclaringType.GetCustomAttribute<ShortClassNameAttribute>()
                                        ?? methodInfo.DeclaringType.Assembly.GetCustomAttribute<ShortClassNameAttribute>();
                // internal for testing
                ResolvedTestClassName = useShortClassName == null ? classType.FullName : classType.Name;

                _testLog = AssemblyTestLog
                    .ForAssembly(classType.GetTypeInfo().Assembly)
                    .StartTestLog(
                        TestOutputHelper,
                        ResolvedTestClassName,
                        out var loggerFactory,
                        logLevelAttribute?.LogLevel ?? LogLevel.Debug,
                        out var resolvedTestName,
                        out var logOutputDirectory,
                        testName);

                ResolvedLogOutputDirectory = logOutputDirectory;
                ResolvedTestMethodName = resolvedTestName;

                LoggerFactory = loggerFactory;
                Logger = loggerFactory.CreateLogger(classType);
            }
            catch (Exception e)
            {
                _initializationException = ExceptionDispatchInfo.Capture(e);
            }
        }

        public virtual void Dispose()
        {
            if(_testLog == null)
            {
                // It seems like sometimes the MSBuild goop that adds the test framework can end up in a bad state and not actually add it
                // Not sure yet why that happens but the exception isn't clear so I'm adding this error so we can detect it better.
                // -anurse
                throw new InvalidOperationException("LoggedTest base class was used but nothing initialized it! The test framework may not be enabled. Try cleaning your 'obj' directory.");
            }

            _initializationException?.Throw();
            _testLog.Dispose();
        }
    }
}
