// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in ConditionalFactAttribute
namespace Microsoft.AspNetCore.Testing
{
    internal class ConditionalFactDiscoverer : FactDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public ConditionalFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
                ? new SkippedTestCase(skipReason, _diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), TestMethodDisplayOptions.None, testMethod)
                : base.CreateTestCase(discoveryOptions, testMethod, factAttribute);
        }
    }
}
