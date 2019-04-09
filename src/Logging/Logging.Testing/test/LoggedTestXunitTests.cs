// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
    [LogLevel(LogLevel.Debug)]
    [ShortClassName]
    public class LoggedTestXunitTests : TestLoggedTest
    {
        private readonly ITestOutputHelper _output;

        public LoggedTestXunitTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShortClassNameUsedWhenShortClassNameAttributeSpecified()
        {
            Assert.Equal(GetType().Name, ResolvedTestClassName);
        }

        [Fact]
        public void LoggedTestTestOutputHelperSameInstanceAsInjectedConstructorArg()
        {
            Assert.Same(_output, TestOutputHelper);
        }

        [Fact]
        public void LoggedFactInitializesLoggedTestProperties()
        {
            Assert.NotNull(Logger);
            Assert.NotNull(LoggerFactory);
            Assert.NotNull(TestSink);
            Assert.NotNull(TestOutputHelper);
        }

        [Theory]
        [InlineData("Hello world")]
        public void LoggedTheoryInitializesLoggedTestProperties(string argument)
        {
            Assert.NotNull(Logger);
            Assert.NotNull(LoggerFactory);
            Assert.NotNull(TestSink);
            Assert.NotNull(TestOutputHelper);
            // Use the test argument
            Assert.NotNull(argument);
        }

        [ConditionalFact]
        public void ConditionalLoggedFactGetsInitializedLoggerFactory()
        {
            Assert.NotNull(Logger);
            Assert.NotNull(LoggerFactory);
            Assert.NotNull(TestSink);
            Assert.NotNull(TestOutputHelper);
        }

        [ConditionalTheory]
        [InlineData("Hello world")]
        public void LoggedConditionalTheoryInitializesLoggedTestProperties(string argument)
        {
            Assert.NotNull(Logger);
            Assert.NotNull(LoggerFactory);
            Assert.NotNull(TestSink);
            Assert.NotNull(TestOutputHelper);
            // Use the test argument
            Assert.NotNull(argument);
        }

        [Fact]
        [LogLevel(LogLevel.Information)]
        public void LoggedFactFilteredByMethodLogLevel()
        {
            Logger.LogInformation("Information");
            Logger.LogDebug("Debug");

            var message = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Information, message.LogLevel);
            Assert.Equal("Information", message.Formatter(message.State, null));
        }

        [Fact]
        public void LoggedFactFilteredByClassLogLevel()
        {
            Logger.LogDebug("Debug");
            Logger.LogTrace("Trace");

            var message = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Debug, message.LogLevel);
            Assert.Equal("Debug", message.Formatter(message.State, null));
        }

        [Theory]
        [InlineData("Hello world")]
        [LogLevel(LogLevel.Information)]
        public void LoggedTheoryFilteredByLogLevel(string argument)
        {
            Logger.LogInformation("Information");
            Logger.LogDebug("Debug");

            var message = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Information, message.LogLevel);
            Assert.Equal("Information", message.Formatter(message.State, null));

            // Use the test argument
            Assert.NotNull(argument);
        }

        [Fact]
        public void AddTestLoggingUpdatedWhenLoggerFactoryIsSet()
        {
            var loggerFactory = new LoggerFactory();
            var serviceCollection = new ServiceCollection();

            LoggerFactory = loggerFactory;
            AddTestLogging(serviceCollection);

            Assert.Same(loggerFactory, serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>());
        }

        [ConditionalTheory]
        [EnvironmentVariableSkipCondition("ASPNETCORE_TEST_LOG_DIR", "")] // The test name is only generated when logging is enabled via the environment variable
        [InlineData(null)]
        public void LoggedTheoryNullArgumentsAreEscaped(string argument)
        {
            Assert.NotNull(LoggerFactory);
            Assert.Equal($"{nameof(LoggedTheoryNullArgumentsAreEscaped)}_null", ResolvedTestMethodName);
            // Use the test argument
            Assert.Null(argument);
        }

        [Fact]
        public void AdditionalSetupInvoked()
        {
            Assert.True(SetupInvoked);
        }

        [Fact]
        public void MessageWrittenEventInvoked()
        {
            WriteContext context = null;
            TestSink.MessageLogged += ctx => context = ctx;
            Logger.LogInformation("Information");
            Assert.Equal(TestSink.Writes.Single(), context);
        }

        [Fact]
        public void ScopeStartedEventInvoked()
        {
            BeginScopeContext context = null;
            TestSink.ScopeStarted += ctx => context = ctx;
            using (Logger.BeginScope("Scope")) {}
            Assert.Equal(TestSink.Scopes.Single(), context);
        }
    }

    public class LoggedTestXunitLogLevelTests : LoggedTest
    {
        [Fact]
        public void LoggedFactFilteredByAssemblyLogLevel()
        {
            Logger.LogTrace("Trace");

            var message = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Trace, message.LogLevel);
            Assert.Equal("Trace", message.Formatter(message.State, null));
        }
    }

    public class LoggedTestXunitInitializationTests : TestLoggedTest
    {
        [Fact]
        public void ITestOutputHelperInitializedByDefault()
        {
            Assert.True(ITestOutputHelperIsInitialized);
        }
    }

    public class TestLoggedTest : LoggedTest
    {
        public bool SetupInvoked { get; private set; } = false;
        public bool ITestOutputHelperIsInitialized { get; private set; } = false;

        public override void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(methodInfo, testMethodArguments, testOutputHelper);

            try
            {
                TestOutputHelper.WriteLine("Test");
                ITestOutputHelperIsInitialized = true;
            } catch { }
            SetupInvoked = true;
        }
    }
}
