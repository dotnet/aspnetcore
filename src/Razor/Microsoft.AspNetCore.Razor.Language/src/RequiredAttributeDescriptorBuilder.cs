// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RequiredAttributeDescriptorBuilder
{
    public abstract string Name { get; set; }

    public abstract RequiredAttributeDescriptor.NameComparisonMode NameComparisonMode { get; set; }

    public abstract string Value { get; set; }

    public abstract RequiredAttributeDescriptor.ValueComparisonMode ValueComparisonMode { get; set; }

    public abstract RazorDiagnosticCollection Diagnostics { get; }

    public virtual IDictionary<string, string> Metadata { get; }
}
