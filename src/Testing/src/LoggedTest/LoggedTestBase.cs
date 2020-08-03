// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Serilog;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Testing
{
    public class LoggedTestBase : ILoggedTest, ITestMethodLifecycle
    {
        private ExceptionDispatchInfo _initializationException;

        private IDisposable _testLog;

        // Obsolete but keeping for back compat
        public LoggedTestBase(ITestOutputHelper output = null)
        {
            TestOutputHelper = output;
        }

        protected TestContext Context { get; private set; }

        // Internal for testing
        internal string ResolvedTestClassName { get; set; }

        public string ResolvedLogOutputDirectory { get; set; }

        public string ResolvedTestMethodName { get; set; }

        public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

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

        public virtual void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            try
            {
                TestOutputHelper = testOutputHelper;

                var classType = GetType();
                var logLevelAttribute = methodInfo.GetCustomAttribute<LogLevelAttribute>()
                                        ?? methodInfo.DeclaringType.GetCustomAttribute<LogLevelAttribute>()
                                        ?? methodInfo.DeclaringType.Assembly.GetCustomAttribute<LogLevelAttribute>();

                // internal for testing
                ResolvedTestClassName = context.FileOutput.TestClassName;

                _testLog = AssemblyTestLog
                    .ForAssembly(classType.GetTypeInfo().Assembly)
                    .StartTestLog(
                        TestOutputHelper,
                        context.FileOutput.TestClassName,
                        out var loggerFactory,
                        logLevelAttribute?.LogLevel ?? LogLevel.Debug,
                        out var resolvedTestName,
                        out var logDirectory,
                        context.FileOutput.TestName);

                ResolvedLogOutputDirectory = logDirectory;
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
            if (_testLog == null)
            {
                // It seems like sometimes the MSBuild goop that adds the test framework can end up in a bad state and not actually add it
                // Not sure yet why that happens but the exception isn't clear so I'm adding this error so we can detect it better.
                // -anurse
                throw new InvalidOperationException("LoggedTest base class was used but nothing initialized it! The test framework may not be enabled. Try cleaning your 'obj' directory.");
            }

            _initializationException?.Throw();
            _testLog.Dispose();
        }

        Task ITestMethodLifecycle.OnTestStartAsync(TestContext context, CancellationToken cancellationToken)
        {

            Context = context;

            Initialize(context, context.TestMethod, context.MethodArguments, context.Output);
            return Task.CompletedTask;
        }

        Task ITestMethodLifecycle.OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
