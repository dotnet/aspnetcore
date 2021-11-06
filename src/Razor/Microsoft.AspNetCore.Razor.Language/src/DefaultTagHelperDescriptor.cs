// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultTagHelperDescriptor : TagHelperDescriptor
{
    public DefaultTagHelperDescriptor(
        string kind,
        string name,
        string assemblyName,
        string displayName,
        string documentation,
        string tagOutputHint,
        bool caseSensitive,
        TagMatchingRuleDescriptor[] tagMatchingRules,
        BoundAttributeDescriptor[] attributeDescriptors,
        AllowedChildTagDescriptor[] allowedChildTags,
        Dictionary<string, string> metadata,
        RazorDiagnostic[] diagnostics)
        : base(kind)
    {
        Name = name;
        AssemblyName = assemblyName;
        DisplayName = displayName;
        Documentation = documentation;
        TagOutputHint = tagOutputHint;
        CaseSensitive = caseSensitive;
        TagMatchingRules = tagMatchingRules;
        BoundAttributes = attributeDescriptors;
        AllowedChildTags = allowedChildTags;
        Diagnostics = diagnostics;
        Metadata = metadata;
    }
}
