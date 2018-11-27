// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    internal class ForegroundTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly TheoryDiscoverer _inner;

        public ForegroundTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _inner = new TheoryDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return _inner.Discover(discoveryOptions, testMethod, factAttribute).Select(t => new ForegroundFactTestCase(t));
        }
    }
}