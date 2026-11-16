// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in ConditionalTheoryAttribute
namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class ConditionalTheoryDiscoverer : TheoryDiscoverer
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
        QuarantinedTestData quarantinedTestData = null;
        if (dataRow?.FirstOrDefault() is QuarantinedTestData quarantine)
        {
            quarantinedTestData = quarantine;
            dataRow = dataRow.Skip(1).ToArray();
        }

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

        var testCases = (skipReason != null ?
            base.CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipReason)
            : base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow)).ToArray();

        if (quarantinedTestData is not null)
        {
            foreach (var testCase in testCases)
            {
                AddQuarantinedTrait(testCase.Traits, quarantinedTestData.Attribute);
            }
        }

        return testCases;
    }

    internal static void AddQuarantinedTrait(IDictionary<string, List<string>> traits, QuarantinedTestAttribute attribute)
    {
        if (!QuarantinedTestTraitDiscoverer.IsQuarantined(attribute))
        {
            return;
        }

        if (!traits.TryGetValue("Quarantined", out var values))
        {
            values = new List<string>();
            traits.Add("Quarantined", values);
        }

        values.Add("true");
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
