// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestInvoker : XunitTestInvoker
    {
        private TestOutputHelper _output;

        public LoggedTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override Task BeforeTestMethodInvokedAsync()
        {
            if (_output != null)
            {
                _output.Initialize(MessageBus, Test);
            }

            return base.BeforeTestMethodInvokedAsync();
        }

        protected override async Task AfterTestMethodInvokedAsync()
        {
            await base.AfterTestMethodInvokedAsync();

            if (_output != null)
            {
                _output.Uninitialize();
            }
        }

        protected override object CreateTestClass()
        {
            var testClass = base.CreateTestClass();

            if (testClass is LoggedTest loggedTestClass)
            {
                var classType = loggedTestClass.GetType();
                var logLevelAttribute = TestMethod.GetCustomAttribute<LogLevelAttribute>() as LogLevelAttribute;
                var testName = TestMethodArguments.Aggregate(TestMethod.Name, (a, b) => $"{a}-{(b ?? "null")}");

                // Try resolving ITestOutputHelper from constructor arguments
                loggedTestClass.TestOutputHelper = ConstructorArguments?.SingleOrDefault(a => typeof(ITestOutputHelper).IsAssignableFrom(a.GetType())) as ITestOutputHelper;

                var useShortClassName = TestMethod.DeclaringType.GetCustomAttribute<ShortClassNameAttribute>()
                    ?? TestMethod.DeclaringType.Assembly.GetCustomAttribute<ShortClassNameAttribute>();
                var resolvedClassName = useShortClassName == null ? classType.FullName : classType.Name;
                // None resolved so create a new one and retain a reference to it for initialization/uninitialization
                if (loggedTestClass.TestOutputHelper == null)
                {
                    loggedTestClass.TestOutputHelper = _output = new TestOutputHelper();
                }

                AssemblyTestLog
                    .ForAssembly(classType.GetTypeInfo().Assembly)
                    .StartTestLog(
                        loggedTestClass.TestOutputHelper,
                        resolvedClassName,
                        out var loggerFactory,
                        logLevelAttribute?.LogLevel ?? LogLevel.Trace,
                        out var resolvedTestName,
                        testName);

                // internal for testing
                loggedTestClass.ResolvedTestMethodName = resolvedTestName;
                loggedTestClass.ResolvedTestClassName = resolvedClassName;

                loggedTestClass.LoggerFactory = loggerFactory;
                loggedTestClass.Logger = loggerFactory.CreateLogger(classType);
                loggedTestClass.TestSink = new TestSink();
                loggerFactory.AddProvider(new TestLoggerProvider(loggedTestClass.TestSink));

                loggedTestClass.AdditionalSetup();
            }

            return testClass;
        }
    }
}
