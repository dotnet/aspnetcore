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
    public class AspNetTestCollectionRunner : XunitTestCollectionRunner
    {
        private readonly IDictionary<Type, object> _assemblyFixtureMappings;
        private readonly IMessageSink _diagnosticMessageSink;

        public AspNetTestCollectionRunner(
            Dictionary<Type, object> assemblyFixtureMappings,
            ITestCollection testCollection,
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _assemblyFixtureMappings = assemblyFixtureMappings;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task AfterTestCollectionStartingAsync()
        {
            await base.AfterTestCollectionStartingAsync();

            // note: We pass the assembly fixtures into the runner as ICollectionFixture<> - this seems to work OK without any
            // drawbacks. It's reasonable that we could add IAssemblyFixture<> and related plumbing if it ever became required.
            //
            // The reason for assembly fixture is when we want to start/stop something as the project scope - tests can only be
            // in one test collection at a time.
            foreach (var mapping in _assemblyFixtureMappings)
            {
                CollectionFixtureMappings.Add(mapping.Key, mapping.Value);
            }
        }

        protected override Task BeforeTestCollectionFinishedAsync()
        {
            // We need to remove the assembly fixtures so they won't get disposed.
            foreach (var mapping in _assemblyFixtureMappings)
            {
                CollectionFixtureMappings.Remove(mapping.Key);
            }

            return base.BeforeTestCollectionFinishedAsync();
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
        {
            var runner = new AspNetTestClassRunner(
                testClass,
                @class,
                testCases,
                DiagnosticMessageSink,
                MessageBus,
                TestCaseOrderer,
                new ExceptionAggregator(Aggregator),
                CancellationTokenSource,
                CollectionFixtureMappings);
            return runner.RunAsync();
        }
    }
}
