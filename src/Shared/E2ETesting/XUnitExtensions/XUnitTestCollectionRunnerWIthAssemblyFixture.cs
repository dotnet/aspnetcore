// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting
{
    public class XunitTestCollectionRunnerWithAssemblyFixture : XunitTestCollectionRunner
    {
        private readonly IDictionary<Type, object> _assemblyFixtureMappings;
        private readonly IMessageSink _diagnosticMessageSink;

        public XunitTestCollectionRunnerWithAssemblyFixture(Dictionary<Type, object> assemblyFixtureMappings,
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

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
        {
            var runner = new XunitTestClassRunner(
                testClass,
                @class,
                testCases,
                _diagnosticMessageSink,
                MessageBus,
                TestCaseOrderer,
                new ExceptionAggregator(Aggregator),
                CancellationTokenSource,
                _assemblyFixtureMappings);

            return runner.RunAsync();
        }
    }
}
