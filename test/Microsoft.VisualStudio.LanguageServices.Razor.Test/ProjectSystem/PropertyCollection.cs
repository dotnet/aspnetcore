// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class PropertyCollection
    {
        private readonly string _ruleName;
        private readonly Dictionary<string, string> _properties;

        public PropertyCollection(string ruleName)
        {
            _ruleName = ruleName;
            _properties = new Dictionary<string, string>();
        }

        public void Property(string key)
        {
            _properties[key] = null;
        }

        public void Property(string key, string value)
        {
            _properties[key] = value;
        }

        public TestProjectRuleSnapshot ToSnapshot()
        {
            return TestProjectRuleSnapshot.CreateProperties(_ruleName, _properties);
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
            return new TestProjectChangeDescription(before, ToSnapshot());
        }
    }
}
