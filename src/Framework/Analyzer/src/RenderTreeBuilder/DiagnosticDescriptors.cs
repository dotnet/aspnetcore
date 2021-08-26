// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor DoNotUseNonLiteralSequenceNumbers = new(
        "ASP0005",
        "Do not use non-literal sequence numbers",
        "'{0}' should not be used as a sequence number",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");
}
