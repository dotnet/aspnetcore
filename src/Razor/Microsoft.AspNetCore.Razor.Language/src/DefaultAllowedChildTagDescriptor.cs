// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultAllowedChildTagDescriptor : AllowedChildTagDescriptor
{
    public DefaultAllowedChildTagDescriptor(string name, string displayName, RazorDiagnostic[] diagnostics)
    {
        Name = name;
        DisplayName = displayName;
        Diagnostics = diagnostics;
    }
}
