// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class TagHelperBinding
    {
        private IReadOnlyDictionary<TagHelperDescriptor, IEnumerable<TagMatchingRuleDescriptor>> _mappings;

        internal TagHelperBinding(
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTagName,
            IReadOnlyDictionary<TagHelperDescriptor, IEnumerable<TagMatchingRuleDescriptor>> mappings,
            string tagHelperPrefix)
        {
            TagName = tagName;
            Attributes = attributes;
            ParentTagName = parentTagName;
            TagHelperPrefix = tagHelperPrefix;

            _mappings = mappings;
        }

        public IEnumerable<TagHelperDescriptor> Descriptors => _mappings.Keys;

        public string TagName { get; }

        public string ParentTagName { get; }

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; }

        public string TagHelperPrefix { get; }

        public IEnumerable<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor)
        {
            return _mappings[descriptor];
        }
    }
}