// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.MinimalActions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    internal static class DiagnosticDescriptors
    {
        internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnMinimalActionParameters = new(
            "ASP0003",
            "Do not use model binding attributes with minimal actions",
            "Attribute '{0}' should not be specified for a minimal action parameter",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/minimal-action/analyzer");

        internal static readonly DiagnosticDescriptor RouteValueIsUnused = new(
            "ASP0004",
            "Route value is unused",
            "The route value '{0}' does not get bound and can be removed",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/minimal-action/analyzer");

        internal static readonly DiagnosticDescriptor RouteParameterCannotBeBound = new(
            "ASP0005",
            "Route parameter is not bound",
            "Route parameter does not have a corresponding route token and cannot be bound",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/minimal-action/analyzer");


    }
}
