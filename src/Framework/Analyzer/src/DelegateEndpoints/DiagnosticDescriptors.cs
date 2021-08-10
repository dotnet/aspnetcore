// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    internal static class DiagnosticDescriptors
    {
        internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnDelegateEndpointParameters = new(
            "ASP0003",
            "Do not use model binding attributes with Map handlers",
            "{0} should not be specified for a {1} Delegate parameter",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        internal static readonly DiagnosticDescriptor DoNotReturnActionResultsFromMapActions = new(
            "ASP0004",
            "Do not use action results with Map actions",
            "IActionResult instances should not be returned from a {0} Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        internal static readonly DiagnosticDescriptor RouteValueIsUnused = new(
            "ASP0005",
            "Route value is unused",
            "The route value '{0}' does not get bound and can be removed",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/minimal-action/analyzer");

        internal static readonly DiagnosticDescriptor RouteParameterCannotBeBound = new(
            "ASP0006",
            "Route parameter is not bound",
            "Route parameter does not have a corresponding route token and cannot be bound",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/minimal-action/analyzer");
    }
}
