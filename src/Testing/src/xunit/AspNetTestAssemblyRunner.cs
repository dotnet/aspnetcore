// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

public class AspNetTestAssemblyRunner : XunitTestAssemblyRunner
{
    private readonly Dictionary<Type, object> _assemblyFixtureMappings = new();

    public AspNetTestAssemblyRunner(
        ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
        : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
    }

    // internal for testing
    internal IEnumerable<object> Fixtures => _assemblyFixtureMappings.Values;

    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync().ConfigureAwait(false);

        // Find all the AssemblyFixtureAttributes on the test assembly
        await Aggregator.RunAsync(async () =>
        {
            var assembly = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly;
            var fixturesAttributes = assembly
                .GetCustomAttributes(typeof(AssemblyFixtureAttribute), false)
                .Cast<AssemblyFixtureAttribute>()
                .ToList();

            // Instantiate all the fixtures
            foreach (var fixtureAttribute in fixturesAttributes)
            {
                object instance = null;
                var staticCreator = fixtureAttribute.FixtureType.GetMethod(
                    name: "ForAssembly",
                    bindingAttr: BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(Assembly) },
                    modifiers: null);
                if (staticCreator is null)
                {
                    var ctorWithDiagnostics = fixtureAttribute
                        .FixtureType
                        .GetConstructor(new[] { typeof(IMessageSink) });
                    if (ctorWithDiagnostics is null)
                    {
                        instance = Activator.CreateInstance(fixtureAttribute.FixtureType);
                    }
                    else
                    {
                        instance = Activator.CreateInstance(fixtureAttribute.FixtureType, DiagnosticMessageSink);
                    }
                }
                else
                {
                    instance = staticCreator.Invoke(obj: null, parameters: new[] { assembly });
                }

                _assemblyFixtureMappings[fixtureAttribute.FixtureType] = instance;

                if (instance is IAsyncLifetime asyncInit)
                {
                    await asyncInit.InitializeAsync().ConfigureAwait(false);
                }
            }
        }).ConfigureAwait(false);
    }

    protected override async Task BeforeTestAssemblyFinishedAsync()
    {
        // Dispose fixtures
        foreach (var disposable in Fixtures.OfType<IDisposable>())
        {
            Aggregator.Run(disposable.Dispose);
        }

        foreach (var disposable in Fixtures.OfType<IAsyncLifetime>())
        {
            await Aggregator.RunAsync(disposable.DisposeAsync).ConfigureAwait(false);
        }

        await base.BeforeTestAssemblyFinishedAsync().ConfigureAwait(false);
    }

    protected override async Task<RunSummary> RunTestCollectionAsync(
        IMessageBus messageBus,
        ITestCollection testCollection,
        IEnumerable<IXunitTestCase> testCases,
        CancellationTokenSource cancellationTokenSource)
    {
        var runSummary = await new AspNetTestCollectionRunner(
            _assemblyFixtureMappings,
            testCollection,
            testCases,
            DiagnosticMessageSink,
            messageBus,
            TestCaseOrderer,
            new ExceptionAggregator(Aggregator),
            cancellationTokenSource)
            .RunAsync()
            .ConfigureAwait(false);
        if (runSummary.Failed != 0)
        {
            foreach (var fixture in Fixtures.OfType<IAcceptFailureReports>())
            {
                fixture.ReportTestFailure();
            }
        }

        return runSummary;
    }
}
