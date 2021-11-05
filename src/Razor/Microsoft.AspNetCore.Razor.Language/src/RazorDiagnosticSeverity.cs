// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public enum RazorDiagnosticSeverity
{
    // Purposely using the same value as Roslyn here.
    Warning = 2,
    Error = 3,
}
