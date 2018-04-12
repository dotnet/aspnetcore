// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ItemCollection
    {
        private readonly string _ruleName;
        private readonly Dictionary<string, Dictionary<string, string>> _items;

        public ItemCollection(string ruleName)
        {
            _ruleName = ruleName;
            _items = new Dictionary<string, Dictionary<string, string>>();
        }

        public void Item(string item)
        {
            Item(item, new Dictionary<string, string>());
        }

        public void Item(string item, Dictionary<string, string> properties)
        {
            _items[item] = properties;
        }

        public void RemoveItem(string item)
        {
            _items.Remove(item);
        }

        public void Property(string item, string key)
        {
            _items[item][key] = null;
        }

        public void Property(string item, string key, string value)
        {
            _items[item][key] = value;
        }

        public TestProjectRuleSnapshot ToSnapshot()
        {
            return TestProjectRuleSnapshot.CreateItems(_ruleName, _items);
        }

        public TestProjectChangeDescription ToChange()
        {
            return ToChange(new TestProjectRuleSnapshot(
                _ruleName,
                ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty,
                ImmutableDictionary<string, string>.Empty,
                ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        public TestProjectChangeDescription ToChange(IProjectRuleSnapshot before)
        {
            Assert.Equal(_ruleName, before.RuleName);
            return new TestProjectChangeDescription(before, ToSnapshot());
        }
    }
}
