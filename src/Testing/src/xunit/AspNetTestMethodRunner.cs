// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting.xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class AspNetTestMethodRunner : XunitTestMethodRunner
{
    private readonly object[] _constructorArguments;
    private readonly IMessageSink _diagnosticMessageSink;

    public AspNetTestMethodRunner(
        ITestMethod testMethod,
        IReflectionTypeInfo @class,
        IReflectionMethodInfo method,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        object[] constructorArguments)
        : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _constructorArguments = constructorArguments;
    }

    protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
    {
        if (testCase.GetType() == typeof(XunitTestCase))
        {
            // If we get here this is a 'regular' test case, not something that represents a skipped test.
            //
            // We can take control of it's invocation thusly.
            var runner = new AspNetTestCaseRunner(
                testCase,
                testCase.DisplayName,
                testCase.SkipReason,
                _constructorArguments,
                testCase.TestMethodArguments,
                MessageBus,
                new ExceptionAggregator(Aggregator),
                CancellationTokenSource);
            return runner.RunAsync();
        }

        if (testCase.GetType() == typeof(XunitTheoryTestCase))
        {
            // If we get here this is a 'regular' theory test case, not something that represents a skipped test.
            //
            // We can take control of it's invocation thusly.
            var runner = new AspNetTheoryTestCaseRunner(
                testCase,
                testCase.DisplayName,
                testCase.SkipReason,
                _constructorArguments,
                _diagnosticMessageSink,
                MessageBus,
                new ExceptionAggregator(Aggregator),
                CancellationTokenSource);
            return runner.RunAsync();
        }

        return base.RunTestCaseAsync(testCase);
    }
}
