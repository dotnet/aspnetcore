// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in ConditionalTheoryAttribute
namespace Microsoft.AspNetCore.Testing
{
    internal class ConditionalTheoryDiscoverer : TheoryDiscoverer
    {
        public ConditionalTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        private sealed class OptionsWithPreEnumerationEnabled : ITestFrameworkDiscoveryOptions
        {
            private const string PreEnumerateTheories = "xunit.discovery.PreEnumerateTheories";

            private readonly ITestFrameworkDiscoveryOptions _original;

            public OptionsWithPreEnumerationEnabled(ITestFrameworkDiscoveryOptions original)
                => _original = original;

            public TValue GetValue<TValue>(string name)
                => (name == PreEnumerateTheories) ? (TValue)(object)true : _original.GetValue<TValue>(name);

            public void SetValue<TValue>(string name, TValue value)
                => _original.SetValue(name, value);
        }

        public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
            => base.Discover(new OptionsWithPreEnumerationEnabled(discoveryOptions), testMethod, theoryAttribute);

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
               ? new[] { new SkippedTestCase(skipReason, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), TestMethodDisplayOptions.None, testMethod) }
               : base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
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

            return skipReason != null ?
                base.CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipReason)
                : base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow);
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute,
            object[] dataRow,
            string skipReason)
        {
            return new[]
            {
                new WORKAROUND_SkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, skipReason, dataRow),
            };
        }

        [Obsolete]
        protected override IXunitTestCase CreateTestCaseForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow, string skipReason)
        {
            return new WORKAROUND_SkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, skipReason, dataRow);
        }
    }
}
