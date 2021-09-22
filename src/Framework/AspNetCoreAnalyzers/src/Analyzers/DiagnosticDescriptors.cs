// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    internal static class DiagnosticDescriptors
    {
        internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnRouteHandlerParameters = new(
            "ASP0003",
            "Do not use model binding attributes with route handlers",
            "{0} should not be specified for a {1} Delegate parameter",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        internal static readonly DiagnosticDescriptor DoNotReturnActionResultsFromRouteHandlers = new(
            "ASP0004",
            "Do not use action results with route handlers",
            "IActionResult instances should not be returned from a {0} Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        internal static readonly DiagnosticDescriptor DetectMisplacedLambdaAttribute = new(
            "ASP0005",
            "Do not place attribute on method called by route handler lambda",
            "'{0}' should be placed directly on the route handler lambda to be effective",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        internal static readonly DiagnosticDescriptor DetectMismatchedParameterOptionality = new(
            "ASP0006",
            "Route parameter and argument optionality is mismatched",
            "'{0}' argument should be annotated as optional or nullable to match route parameter",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");
    }
}
