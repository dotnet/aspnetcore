// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    public class AspNetTestAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly Dictionary<Type, object> _assemblyFixtureMappings = new Dictionary<Type, object>();

        public AspNetTestAssemblyRunner(
            ITestAssembly testAssembly,
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override async Task AfterTestAssemblyStartingAsync()
        {
            await base.AfterTestAssemblyStartingAsync();

            // Find all the AssemblyFixtureAttributes on the test assembly
            Aggregator.Run(() =>
            {
                var fixturesAttributes = ((IReflectionAssemblyInfo)TestAssembly.Assembly)
                    .Assembly
                    .GetCustomAttributes(typeof(AssemblyFixtureAttribute), false)
                    .Cast<AssemblyFixtureAttribute>()
                    .ToList();

                // Instantiate all the fixtures
                foreach (var fixtureAttribute in fixturesAttributes)
                {
                    var ctorWithDiagnostics = fixtureAttribute.FixtureType.GetConstructor(new[] { typeof(IMessageSink) });
                    if (ctorWithDiagnostics != null)
                    {
                        _assemblyFixtureMappings[fixtureAttribute.FixtureType] = Activator.CreateInstance(fixtureAttribute.FixtureType, DiagnosticMessageSink);
                    }
                    else
                    {
                        _assemblyFixtureMappings[fixtureAttribute.FixtureType] = Activator.CreateInstance(fixtureAttribute.FixtureType);
                    }
                }
            });
        }

        protected override Task BeforeTestAssemblyFinishedAsync()
        {
            // Dispose fixtures
            foreach (var disposable in _assemblyFixtureMappings.Values.OfType<IDisposable>())
            {
                Aggregator.Run(disposable.Dispose);
            }

            return base.BeforeTestAssemblyFinishedAsync();
        }

        protected override Task<RunSummary> RunTestCollectionAsync(
            IMessageBus messageBus,
            ITestCollection testCollection,
            IEnumerable<IXunitTestCase> testCases,
            CancellationTokenSource cancellationTokenSource)
            => new AspNetTestCollectionRunner(
                _assemblyFixtureMappings,
                testCollection,
                testCases,
                DiagnosticMessageSink,
                messageBus,
                TestCaseOrderer,
                new ExceptionAggregator(Aggregator),
                cancellationTokenSource).RunAsync();
    }
}
