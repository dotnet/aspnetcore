// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class TagMatchingRuleDescriptorBuilder
    {
        public abstract TagMatchingRuleDescriptorBuilder RequireTagName(string tagName);

        public abstract TagMatchingRuleDescriptorBuilder RequireParentTag(string parentTag);

        public abstract TagMatchingRuleDescriptorBuilder RequireTagStructure(TagStructure tagStructure);

        public abstract TagMatchingRuleDescriptorBuilder RequireAttribute(Action<RequiredAttributeDescriptorBuilder> configure);

        public abstract TagMatchingRuleDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic);
    }
}
