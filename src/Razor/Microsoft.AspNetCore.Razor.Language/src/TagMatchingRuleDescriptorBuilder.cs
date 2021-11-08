// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class TagMatchingRuleDescriptorBuilder
{
    public abstract string TagName { get; set; }

    public abstract string ParentTag { get; set; }

    public abstract TagStructure TagStructure { get; set; }

    public abstract RazorDiagnosticCollection Diagnostics { get; }

    public abstract IReadOnlyList<RequiredAttributeDescriptorBuilder> Attributes { get; }

    public abstract void Attribute(Action<RequiredAttributeDescriptorBuilder> configure);
}
