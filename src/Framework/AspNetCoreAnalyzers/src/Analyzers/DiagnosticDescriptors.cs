// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

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

    internal static readonly DiagnosticDescriptor DoNotUseNonLiteralSequenceNumbers = new(
        "ASP0006",
        "Do not use non-literal sequence numbers",
        "'{0}' should not be used as a sequence number. Instead, use an integer literal representing source code order.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DetectMismatchedParameterOptionality = new(
        "ASP0007",
        "Route parameter and argument optionality is mismatched",
        "'{0}' argument should be annotated as optional or nullable to match route parameter",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor CustomBindingBindAsyncMustHaveAValidFormat = new(
        "ASP0008",
        "Custom binding BindAsync method must be of a valid format to be effective",
        "BindAsync method found on '{0}' with incorrect format. Must be public static with format, ValueTask<{0}> BindAsync(HttpContext, ParameterInfo) or ValueTask<{0}> BindAsync(HttpContext).",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor CustomBindingTryParseMustHaveAValidFormat = new(
        "ASP0009",
        "Custom binding TryParse method must be of a valid format to be effective",
        "TryParse method found on '{0}' with incorrect format. Must be public static with format, bool TryParse(string, IFormatProvider, out {0}) or bool TryParse(string, out {0}).",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor CustomBindingMethodMustBePublic = new(
        "ASP0010",
        "Custom binding method method must be public to be effective",
        "{0} must be public to be effective as a custom binding",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor CustomBindingMethodMustBeStatic = new(
        "ASP0011",
        "Custom binding method must be static to be effective",
        "{0} must be static to be effective as a custom binding",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");
}
