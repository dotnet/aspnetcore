// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

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
