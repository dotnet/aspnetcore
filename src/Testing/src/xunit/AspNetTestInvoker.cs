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

namespace Microsoft.AspNetCore.Testing
{
    internal class AspNetTestInvoker : XunitTestInvoker
    {
        private readonly TestOutputHelper _testOutputHelper;

        public AspNetTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            TestOutputHelper testOutputHelper)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var context = new TestContext(TestClass, ConstructorArguments, TestMethod, TestMethodArguments, _testOutputHelper);
            var lifecycleHooks = GetLifecycleHooks(testClassInstance, TestClass, TestMethod);

            await Aggregator.RunAsync(async () =>
            {
                foreach (var lifecycleHook in lifecycleHooks)
                {
                    await lifecycleHook.OnTestStartAsync(context, CancellationTokenSource.Token);
                }
            });

            var retryAttribute = GetRetryAttribute(TestMethod);
            var time = 0.0M;
            if (retryAttribute == null)
            {
                time = await base.InvokeTestMethodAsync(testClassInstance);
            }
            else
            {
                time = await RetryAsync(retryAttribute, testClassInstance);
            }

            await Aggregator.RunAsync(async () =>
            {
                var exception = Aggregator.HasExceptions ? Aggregator.ToException() : null;
                foreach (var lifecycleHook in lifecycleHooks)
                {
                    await lifecycleHook.OnTestEndAsync(context, exception, CancellationTokenSource.Token);
                }
            });

            return time;
        }

        protected async Task<decimal> RetryAsync(RetryAttribute retryAttribute, object testClassInstance)
        {
            var attempts = 0;
            var timeTaken = 0.0M;
            for (attempts = 0; attempts < retryAttribute.MaxRetries; attempts++)
            {
                timeTaken = await base.InvokeTestMethodAsync(testClassInstance);
                if (!Aggregator.HasExceptions)
                {
                    return timeTaken;
                }
                else if (attempts < retryAttribute.MaxRetries - 1)
                {
                    _testOutputHelper.WriteLine($"Retrying test, attempt {attempts} of {retryAttribute.MaxRetries} failed.");
                    await Task.Delay(5000);
                    Aggregator.Clear();
                }
            }

            return timeTaken;
        }

        private RetryAttribute GetRetryAttribute(MethodInfo methodInfo)
        {
            var attributeCandidate = methodInfo.GetCustomAttribute<RetryAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            attributeCandidate = methodInfo.DeclaringType.GetCustomAttribute<RetryAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            return methodInfo.DeclaringType.Assembly.GetCustomAttribute<RetryAttribute>();
        }

        private static IEnumerable<ITestMethodLifecycle> GetLifecycleHooks(object testClassInstance, Type testClass, MethodInfo testMethod)
        {
            foreach (var attribute in testMethod.GetCustomAttributes(inherit: true).OfType<ITestMethodLifecycle>())
            {
                yield return attribute;
            }

            if (testClassInstance is ITestMethodLifecycle instance)
            {
                yield return instance;
            }

            foreach (var attribute in testClass.GetCustomAttributes(inherit: true).OfType<ITestMethodLifecycle>())
            {
                yield return attribute;
            }

            foreach (var attribute in testClass.Assembly.GetCustomAttributes(inherit: true).OfType<ITestMethodLifecycle>())
            {
                yield return attribute;
            }
        }
    }
}
