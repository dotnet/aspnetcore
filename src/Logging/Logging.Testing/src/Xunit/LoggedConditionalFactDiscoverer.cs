// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedConditionalFactDiscoverer : LoggedFactDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public LoggedConditionalFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
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
