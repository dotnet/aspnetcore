// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class TagMatchingRuleDescriptorBuilder
    {
        public abstract string TagName { get; set; }

        public abstract string ParentTag { get; set; }

        public abstract TagStructure TagStructure { get; set; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }

        public abstract IReadOnlyList<RequiredAttributeDescriptorBuilder> Attributes { get; }

        public abstract void Attribute(Action<RequiredAttributeDescriptorBuilder> configure);
    }
}
