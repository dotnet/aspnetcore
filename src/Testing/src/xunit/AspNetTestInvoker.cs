// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class AspNetTestInvoker : XunitTestInvoker
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
                await lifecycleHook.OnTestStartAsync(context, CancellationTokenSource.Token).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        var time = await base.InvokeTestMethodAsync(testClassInstance).ConfigureAwait(false);

        await Aggregator.RunAsync(async () =>
        {
            var exception = Aggregator.HasExceptions ? Aggregator.ToException() : null;
            foreach (var lifecycleHook in lifecycleHooks)
            {
                await lifecycleHook.OnTestEndAsync(context, exception, CancellationTokenSource.Token).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

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
