// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class TagHelperBinding
    {
        private IReadOnlyDictionary<TagHelperDescriptor, IReadOnlyList<TagMatchingRuleDescriptor>> _mappings;

        internal TagHelperBinding(
            string tagName,
            IReadOnlyList<KeyValuePair<string, string>> attributes,
            string parentTagName,
            IReadOnlyDictionary<TagHelperDescriptor, IReadOnlyList<TagMatchingRuleDescriptor>> mappings,
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

        public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

        public string TagHelperPrefix { get; }

        public IReadOnlyList<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor)
        {
            return _mappings[descriptor];
        }
    }
}