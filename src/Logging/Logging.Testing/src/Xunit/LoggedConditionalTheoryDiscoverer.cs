// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedConditionalTheoryDiscoverer : LoggedTheoryDiscoverer
    {
        public LoggedConditionalTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
               ? new[] { new SkippedTestCase(skipReason, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod) }
               : base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod, IAttributeInfo theoryAttribute,
            object[] dataRow)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            if (skipReason == null && dataRow?.Length > 0)
            {
                var obj = dataRow[0];
                if (obj != null)
                {
                    var type = obj.GetType();
                    var property = type.GetProperty("Skip");
                    if (property != null && property.PropertyType.Equals(typeof(string)))
                    {
                        skipReason = property.GetValue(obj) as string;
                    }
                }
            }

            return skipReason != null
                ? base.CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipReason)
                : base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow);
        }

    }
}
