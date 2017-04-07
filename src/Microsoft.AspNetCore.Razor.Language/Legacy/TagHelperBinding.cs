// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public sealed class TagHelperBinding
    {
        public IReadOnlyDictionary<TagHelperDescriptor, IEnumerable<TagMatchingRule>> _mappings;

        internal TagHelperBinding(IReadOnlyDictionary<TagHelperDescriptor, IEnumerable<TagMatchingRule>> mappings)
        {
            _mappings = mappings;
            Descriptors = _mappings.Keys;
        }

        public IEnumerable<TagHelperDescriptor> Descriptors { get; }

        public IEnumerable<TagMatchingRule> GetBoundRules(TagHelperDescriptor descriptor)
        {
            return _mappings[descriptor];
        }
    }
}