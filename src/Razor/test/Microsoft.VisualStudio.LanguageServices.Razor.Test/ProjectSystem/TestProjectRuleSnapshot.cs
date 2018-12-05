// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class TestProjectRuleSnapshot : IProjectRuleSnapshot
    {
        public static TestProjectRuleSnapshot CreateProperties(string ruleName, Dictionary<string, string> properties)
        {
            return new TestProjectRuleSnapshot(
                ruleName,
                items: ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty,
                properties: properties.ToImmutableDictionary(),
                dataSourceVersions: ImmutableDictionary<NamedIdentity, IComparable>.Empty);
        }

        public static TestProjectRuleSnapshot CreateItems(string ruleName, Dictionary<string, Dictionary<string, string>> items)
        {
            return new TestProjectRuleSnapshot(
                ruleName,
                items: items.ToImmutableDictionary(kvp => kvp.Key, kvp => (IImmutableDictionary<string, string>)kvp.Value.ToImmutableDictionary()),
                properties: ImmutableDictionary<string, string>.Empty,
                dataSourceVersions: ImmutableDictionary<NamedIdentity, IComparable>.Empty);
        }

        public TestProjectRuleSnapshot(
            string ruleName,
            IImmutableDictionary<string, IImmutableDictionary<string, string>> items,
            IImmutableDictionary<string, string> properties,
            IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
        {
            RuleName = ruleName;
            Items = items;
            Properties = properties;
            DataSourceVersions = dataSourceVersions;
        }

        public string RuleName { get; }

        public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items { get; }

        public IImmutableDictionary<string, string> Properties { get; }

        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }
    }
}
