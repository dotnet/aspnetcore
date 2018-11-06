// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestMethodRunner : XunitTestMethodRunner
    {
        private IMessageSink DiagnosticMessageSink { get; }
        private object[] ConstructorArguments { get; }

        public LoggedTestMethodRunner(
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
            DiagnosticMessageSink = diagnosticMessageSink;
            ConstructorArguments = constructorArguments;
        }

        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
            => testCase.RunAsync(DiagnosticMessageSink, MessageBus, ConstructorArguments, new ExceptionAggregator(Aggregator), CancellationTokenSource);
    }
}
