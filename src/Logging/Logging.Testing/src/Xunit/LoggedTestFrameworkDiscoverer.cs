// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestFrameworkDiscoverer : XunitTestFrameworkDiscoverer
    {
        private IDictionary<Type, IXunitTestCaseDiscoverer> Discoverers { get; }

        public LoggedTestFrameworkDiscoverer(
            IAssemblyInfo assemblyInfo,
            ISourceInformationProvider sourceProvider,
            IMessageSink diagnosticMessageSink,
            IXunitTestCollectionFactory collectionFactory = null)
            : base(assemblyInfo, sourceProvider, diagnosticMessageSink, collectionFactory)
        {
            Discoverers = new Dictionary<Type, IXunitTestCaseDiscoverer>()
            {
                { typeof(ConditionalTheoryAttribute), new LoggedConditionalTheoryDiscoverer(diagnosticMessageSink) },
                { typeof(ConditionalFactAttribute), new LoggedConditionalFactDiscoverer(diagnosticMessageSink) },
                { typeof(TheoryAttribute), new LoggedTheoryDiscoverer(diagnosticMessageSink) },
                { typeof(FactAttribute), new LoggedFactDiscoverer(diagnosticMessageSink) }
            };
        }

        protected override bool FindTestsForMethod(
            ITestMethod testMethod,
            bool includeSourceInformation,
            IMessageBus messageBus,
            ITestFrameworkDiscoveryOptions discoveryOptions)
        {
            if (typeof(ILoggedTest).IsAssignableFrom(testMethod.TestClass.Class.ToRuntimeType()))
            {
                var factAttributes = testMethod.Method.GetCustomAttributes(typeof(FactAttribute));
                if (factAttributes.Count() > 1)
                {
                    var message = $"Test method '{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}' has multiple [Fact]-derived attributes";
                    var testCase = new ExecutionErrorTestCase(DiagnosticMessageSink, TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, testMethod, message);
                    return ReportDiscoveredTestCase(testCase, includeSourceInformation, messageBus);
                }

                var factAttribute = factAttributes.FirstOrDefault();
                if (factAttribute == null)
                {
                    return true;
                }

                var factAttributeType = (factAttribute as IReflectionAttributeInfo)?.Attribute.GetType();
                if (!Discoverers.TryGetValue(factAttributeType, out var discoverer))
                {
                    return base.FindTestsForMethod(testMethod, includeSourceInformation, messageBus, discoveryOptions);
                }
                else
                {
                    foreach (var testCase in discoverer.Discover(discoveryOptions, testMethod, factAttribute))
                    {
                        if (!ReportDiscoveredTestCase(testCase, includeSourceInformation, messageBus))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                return base.FindTestsForMethod(testMethod, includeSourceInformation, messageBus, discoveryOptions);
            }
        }
    }
}
