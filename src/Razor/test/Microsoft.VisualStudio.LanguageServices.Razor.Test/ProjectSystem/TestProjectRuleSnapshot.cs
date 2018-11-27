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

        public void SetProperty(string key, string value)
        {
            Properties = Properties.SetItem(key, value);
        }

        public void SetItem(string key, Dictionary<string, string> values)
        {
            Items = Items.SetItem(key, values.ToImmutableDictionary());
        }

        public string RuleName { get; }

        public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items { get; set; }

        public IImmutableDictionary<string, string> Properties { get; set; }

        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }
    }
}
