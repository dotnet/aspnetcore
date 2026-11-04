// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class AspNetTestRunner : XunitTestRunner
{
    private readonly TestOutputHelper _testOutputHelper;
    private readonly bool _ownsTestOutputHelper;

    public AspNetTestRunner(
        ITest test,
        IMessageBus messageBus,
        Type testClass,
        object[] constructorArguments,
        MethodInfo testMethod,
        object[] testMethodArguments,
        string skipReason,
        IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
        : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
    {
        // Prioritize using ITestOutputHelper from constructor.
        if (ConstructorArguments != null)
        {
            foreach (var obj in ConstructorArguments)
            {
                _testOutputHelper = obj as TestOutputHelper;
                if (_testOutputHelper != null)
                {
                    break;
                }
            }
        }

        // No ITestOutputHelper in constructor so we'll create it ourselves.
        if (_testOutputHelper == null)
        {
            _testOutputHelper = new TestOutputHelper();
            _ownsTestOutputHelper = true;
        }
    }

    protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
    {
        if (_ownsTestOutputHelper)
        {
            _testOutputHelper.Initialize(MessageBus, Test);
        }

        var retryAttribute = GetRetryAttribute(TestMethod);
        var result = retryAttribute is null
            ? await base.InvokeTestAsync(aggregator).ConfigureAwait(false)
            : await RunTestCaseWithRetryAsync(retryAttribute, aggregator).ConfigureAwait(false);

        if (_ownsTestOutputHelper)
        {
            // Update result with output if we created our own ITestOutputHelper.
            // The string returned from this method is what VS displays as the test output.
            result = new Tuple<decimal, string>(result.Item1, _testOutputHelper.Output);
            _testOutputHelper.Uninitialize();
        }

        return result;
    }

    private async Task<Tuple<decimal, string>> RunTestCaseWithRetryAsync(RetryAttribute retryAttribute, ExceptionAggregator aggregator)
    {
        var totalTimeTaken = 0m;
        List<string> messages = new();
        var numAttempts = Math.Max(1, retryAttribute.MaxRetries);

        for (var attempt = 1; attempt <= numAttempts; attempt++)
        {
            var result = await base.InvokeTestAsync(aggregator).ConfigureAwait(false);
            totalTimeTaken += result.Item1;
            messages.Add(result.Item2);

            if (!aggregator.HasExceptions)
            {
                break;
            }
            else if (attempt < numAttempts)
            {
                // We can't use the ITestOutputHelper here because there's no active test
                messages.Add($"[{TestCase.DisplayName}] Attempt {attempt} of {retryAttribute.MaxRetries} failed due to {aggregator.ToException()}");

                await Task.Delay(5000).ConfigureAwait(false);
                aggregator.Clear();
            }
        }

        return new(totalTimeTaken, string.Join(Environment.NewLine, messages));
    }

    protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
    {
        var repeatAttribute = GetRepeatAttribute(TestMethod);
        if (repeatAttribute == null)
        {
            return await InvokeTestMethodCoreAsync(aggregator).ConfigureAwait(false);
        }

        var repeatContext = new RepeatContext(repeatAttribute.RunCount);
        RepeatContext.Current = repeatContext;

        var timeTaken = 0.0M;
        for (repeatContext.CurrentIteration = 0; repeatContext.CurrentIteration < repeatContext.Limit; repeatContext.CurrentIteration++)
        {
            timeTaken = await InvokeTestMethodCoreAsync(aggregator).ConfigureAwait(false);
            if (aggregator.HasExceptions)
            {
                return timeTaken;
            }
        }

        return timeTaken;
    }

    private Task<decimal> InvokeTestMethodCoreAsync(ExceptionAggregator aggregator)
    {
        var invoker = new AspNetTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource, _testOutputHelper);
        return invoker.RunAsync();
    }

    private static RepeatAttribute GetRepeatAttribute(MethodInfo methodInfo)
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

    private static RetryAttribute GetRetryAttribute(MethodInfo methodInfo)
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
}
