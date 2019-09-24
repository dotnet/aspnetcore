// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal struct TagHelperSpanInternal
    {
        public TagHelperSpanInternal(SourceSpan span, TagHelperBinding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            Span = span;
            Binding = binding;
        }

        public TagHelperBinding Binding { get; }

        public IEnumerable<TagHelperDescriptor> TagHelpers => Binding.Descriptors;

        public SourceSpan Span { get; }
    }
}
