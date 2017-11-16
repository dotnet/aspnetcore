// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class TagHelperFactsServiceInternal : ILanguageService
    {
        public abstract TagHelperBinding GetTagHelperBinding(TagHelperDocumentContext documentContext, string tagName, IEnumerable<KeyValuePair<string, string>> attributes, string parentTag, bool parentIsTagHelper);

        public abstract IEnumerable<BoundAttributeDescriptor> GetBoundTagHelperAttributes(TagHelperDocumentContext documentContext, string attributeName, TagHelperBinding binding);

        public abstract IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenTag(TagHelperDocumentContext documentContext, string tagName, string parentTag);

        public abstract IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string parentTag);
    }
}
