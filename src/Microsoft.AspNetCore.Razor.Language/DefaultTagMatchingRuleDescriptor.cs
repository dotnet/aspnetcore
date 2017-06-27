// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultTagMatchingRuleDescriptor : TagMatchingRuleDescriptor
    {
        public DefaultTagMatchingRuleDescriptor(
            string tagName,
            string parentTag,
            TagStructure tagStructure,
            RequiredAttributeDescriptor[] attributes,
            RazorDiagnostic[] diagnostics)
        {
            TagName = tagName;
            ParentTag = parentTag;
            TagStructure = tagStructure;
            Attributes = attributes;
            Diagnostics = diagnostics;
        }
    }
}