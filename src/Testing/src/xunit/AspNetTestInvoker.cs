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
        public AspNetTestInvoker(
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

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var output = new TestOutputHelper();
            output.Initialize(MessageBus, Test);

            var context = new TestContext(TestClass, ConstructorArguments, TestMethod, TestMethodArguments, output);
            var lifecycleHooks = GetLifecycleHooks(testClassInstance, TestClass, TestMethod);

            await Aggregator.RunAsync(async () =>
            {
                foreach (var lifecycleHook in lifecycleHooks)
                {
                    await lifecycleHook.OnTestStartAsync(context, CancellationTokenSource.Token);
                }
            });

            var time = await base.InvokeTestMethodAsync(testClassInstance);

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
