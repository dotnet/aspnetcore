// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class BoundAttributeParameterDescriptorBuilder
{
    public abstract string Name { get; set; }

    public abstract string TypeName { get; set; }

    public abstract bool IsEnum { get; set; }

    public abstract string Documentation { get; set; }

    public abstract string DisplayName { get; set; }

    public abstract IDictionary<string, string> Metadata { get; }

    public abstract RazorDiagnosticCollection Diagnostics { get; }
}
