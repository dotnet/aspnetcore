// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultTagMatchingRuleDescriptor : TagMatchingRuleDescriptor
{
    public DefaultTagMatchingRuleDescriptor(
        string tagName,
        string parentTag,
        TagStructure tagStructure,
        bool caseSensitive,
        RequiredAttributeDescriptor[] attributes,
        RazorDiagnostic[] diagnostics)
    {
        TagName = tagName;
        ParentTag = parentTag;
        TagStructure = tagStructure;
        CaseSensitive = caseSensitive;
        Attributes = attributes;
        Diagnostics = diagnostics;
    }
}
