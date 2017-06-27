// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultTagHelperDescriptor : TagHelperDescriptor
    {
        public DefaultTagHelperDescriptor(
            string kind,
            string name,
            string assemblyName,
            string displayName,
            string documentation,
            string tagOutputHint,
            TagMatchingRuleDescriptor[] tagMatchingRules,
            BoundAttributeDescriptor[] attributeDescriptors,
            string[] allowedChildTags,
            Dictionary<string, string> metadata,
            RazorDiagnostic[] diagnostics) 
            : base(kind)
        {
            Name = name;
            AssemblyName = assemblyName;
            DisplayName = displayName;
            Documentation = documentation;
            TagOutputHint = tagOutputHint;
            TagMatchingRules = tagMatchingRules;
            BoundAttributes = attributeDescriptors;
            AllowedChildTags = allowedChildTags;
            Diagnostics = diagnostics;
            Metadata = metadata;
        }
    }
}
