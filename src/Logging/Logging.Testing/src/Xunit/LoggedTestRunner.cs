// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestRunner : XunitTestRunner
    {
        public LoggedTestRunner(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod, object[]
            testMethodArguments, string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected async override Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            var testOutputHelper = ConstructorArguments.SingleOrDefault(a => typeof(TestOutputHelper).IsAssignableFrom(a.GetType())) as TestOutputHelper
                ?? new TestOutputHelper();
            testOutputHelper.Initialize(MessageBus, Test);

            var executionTime = await InvokeTestMethodAsync(aggregator, testOutputHelper);

            var output = testOutputHelper.Output;
            testOutputHelper.Uninitialize();

            return Tuple.Create(executionTime, output);
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
            => InvokeTestMethodAsync(aggregator, null);

        private async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator, ITestOutputHelper output)
        {
            var collectDump = TestMethod.GetCustomAttribute<CollectDumpAttribute>() != null;
            var repeatAttribute = GetRepeatAttribute(TestMethod);
            
            if (!typeof(LoggedTestBase).IsAssignableFrom(TestClass) || repeatAttribute == null)
            {
                return await new LoggedTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource, output, null, collectDump).RunAsync();
            }

            return await RunRepeatTestInvoker(aggregator, output, collectDump, repeatAttribute);
        }

        private async Task<decimal> RunRepeatTestInvoker(ExceptionAggregator aggregator, ITestOutputHelper output, bool collectDump, RepeatAttribute repeatAttribute)
        {
            var repeatContext = new RepeatContext
            {
                Limit = repeatAttribute.RunCount
            };

            var timeTaken = 0.0M;
            var testLogger = new LoggedTestInvoker(
                Test,
                MessageBus,
                TestClass,
                ConstructorArguments,
                TestMethod,
                TestMethodArguments,
                BeforeAfterAttributes,
                aggregator,
                CancellationTokenSource,
                output,
                repeatContext,
                collectDump);

            for (repeatContext.CurrentIteration = 0; repeatContext.CurrentIteration < repeatContext.Limit; repeatContext.CurrentIteration++)
            {
                timeTaken = await testLogger.RunAsync();
                if (aggregator.HasExceptions)
                {
                    return timeTaken;
                }
            }

            return timeTaken;
        }

        private RepeatAttribute GetRepeatAttribute(MethodInfo methodInfo)
        {
            var attributeCandidate = methodInfo.GetCustomAttribute<RepeatAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            attributeCandidate = methodInfo.DeclaringType.GetCustomAttribute<RepeatAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            return methodInfo.DeclaringType.Assembly.GetCustomAttribute<RepeatAttribute>();
        }
    }
}
