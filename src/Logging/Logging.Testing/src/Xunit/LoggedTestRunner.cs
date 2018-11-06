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
using Microsoft.Extensions.Logging.Testing;
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
            var retryAttribute = GetRetryAttribute(TestMethod);
            if (!typeof(LoggedTestBase).IsAssignableFrom(TestClass) || retryAttribute == null)
            {
                return await new LoggedTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource, output, null).RunAsync();
            }

            var retryPredicateMethodName = retryAttribute.RetryPredicateName;
            var retryPredicateMethod = TestClass.GetMethod(retryPredicateMethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null,
                new Type[] { typeof(Exception) },
                null)
                ?? throw new InvalidOperationException($"No valid static retry predicate method {retryPredicateMethodName} was found on the type {TestClass.FullName}.");

            if (retryPredicateMethod.ReturnType != typeof(bool))
            {
                throw new InvalidOperationException($"Retry predicate method {retryPredicateMethodName} on {TestClass.FullName} does not return bool.");
            }

            var retryContext = new RetryContext()
            {
                Limit = retryAttribute.RetryLimit,
                Reason = retryAttribute.RetryReason,
            };

            var retryAggregator = new ExceptionAggregator();
            var loggedTestInvoker = new LoggedTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, retryAggregator, CancellationTokenSource, output, retryContext);
            var totalTime = 0.0M;

            do
            {
                retryAggregator.Clear();
                totalTime += await loggedTestInvoker.RunAsync();
                retryContext.CurrentIteration++;
            }
            while (retryAggregator.HasExceptions
                && retryContext.CurrentIteration < retryContext.Limit
                && (retryPredicateMethod.IsStatic
                    ? (bool)retryPredicateMethod.Invoke(null, new object[] { retryAggregator.ToException() })
                    : (bool)retryPredicateMethod.Invoke(retryContext.TestClassInstance, new object[] { retryAggregator.ToException() }))
                );

            aggregator.Aggregate(retryAggregator);
            return totalTime;
        }


        private RetryTestAttribute GetRetryAttribute(MethodInfo methodInfo)
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OperatingSystems.MacOSX
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OperatingSystems.Windows
                : OperatingSystems.Linux;

            var attributeCandidate = methodInfo.GetCustomAttribute<RetryTestAttribute>();

            if (attributeCandidate != null && (attributeCandidate.OperatingSystems & os) != 0)
            {
                return attributeCandidate;
            }

            attributeCandidate = methodInfo.DeclaringType.GetCustomAttribute<RetryTestAttribute>();

            if (attributeCandidate != null && (attributeCandidate.OperatingSystems & os) != 0)
            {
                return attributeCandidate;
            }

            attributeCandidate = methodInfo.DeclaringType.Assembly.GetCustomAttribute<RetryTestAttribute>();

            if (attributeCandidate != null && (attributeCandidate.OperatingSystems & os) != 0)
            {
                return attributeCandidate;
            }

            return null;
        }
    }
}
