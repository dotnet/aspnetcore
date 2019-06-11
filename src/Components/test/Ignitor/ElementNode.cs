// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Ignitor
{
    internal class ElementNode : ContainerNode
    {
        private readonly Dictionary<string, object> _attributes;
        private readonly Dictionary<string, object> _properties;

        public ElementNode(string tagName)
        {
            TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
            _attributes = new Dictionary<string, object>(StringComparer.Ordinal);
            _properties = new Dictionary<string, object>(StringComparer.Ordinal);
        }
        public string TagName { get; }

        public IReadOnlyDictionary<string, object> Attributes => _attributes;

        public IReadOnlyDictionary<string, object> Properties => _properties;

        public void SetAttribute(string key, object value)
        {
            _attributes[key] = value;
        }

        public void RemoveAttribute(string key)
        {
            _attributes.Remove(key);
        }

        public void SetProperty(string key, object value)
        {
            _properties[key] = value;
        }
    }
}
