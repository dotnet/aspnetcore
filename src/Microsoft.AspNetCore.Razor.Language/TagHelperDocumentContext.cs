// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// The binding information for Tag Helpers resulted to a <see cref="RazorCodeDocument"/>. Represents the
    /// Tag Helper information after processing by directives.
    /// </summary>
    public abstract class TagHelperDocumentContext
    {
        public static TagHelperDocumentContext Create(string prefix, IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            if (tagHelpers == null)
            {
                throw new ArgumentNullException(nameof(tagHelpers));
            }

            return new DefaultTagHelperDocumentContext(prefix, tagHelpers.ToArray());
        }

        public abstract string Prefix { get; }

        public abstract IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        private class DefaultTagHelperDocumentContext : TagHelperDocumentContext
        {
            private readonly string _prefix;
            private readonly TagHelperDescriptor[] _tagHelpers;

            public DefaultTagHelperDocumentContext(string prefix, TagHelperDescriptor[] tagHelpers)
            {
                _prefix = prefix;
                _tagHelpers = tagHelpers;
            }

            public override string Prefix => _prefix;

            public override IReadOnlyList<TagHelperDescriptor> TagHelpers => _tagHelpers;
        }
    }
}
