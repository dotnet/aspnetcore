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

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWebHostWithConfigureHostBuilder = new(
        "ASP0008",
        "Do not use ConfigureWebHost with WebApplicationBuilder.Host",
        "ConfigureWebHost cannot be used with WebApplicationBuilder.Host",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWithConfigureWebHostBuilder = new(
        "ASP0009",
        "Do not use Configure with WebApplicationBuilder.WebHost",
        "Configure cannot be used with WebApplicationBuilder.WebHost",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseUseStartupWithConfigureWebHostBuilder = new(
        "ASP0010",
        "Do not use UseStartup with WebApplicationBuilder.WebHost",
        "UseStartup cannot be used with WebApplicationBuilder.WebHost",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureLogging = new(
        "ASP0011",
        "Suggest using builder.Logging over Host.ConfigureLogging or WebHost.ConfigureLogging",
        "Suggest using builder.Logging instead of {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");
  
    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureServices = new(
        "ASP0012",
        "Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices",
        "Suggest using builder.Services instead of {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DisallowConfigureAppConfigureHostBuilder = new(
        "ASP0013",
        "Suggest switching from using Configure methods to WebApplicationBuilder.Configuration",
        "Suggest using WebApplicationBuilder.Configuration instead of {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor UseTopLevelRouteRegistrationsInsteadOfUseEndpoints = new(
        "ASP0014",
        "Suggest using top level route registrations",
        "Suggest using top level route registrations instead of {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor UseHeaderDictionaryPropertiesInsteadOfIndexer = new(
        "ASP0015",
        new LocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryIndexer_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryIndexer_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotReturnValueFromRequestDelegate = new(
        "ASP0016",
        new LocalizableResourceString(nameof(Resources.Analyzer_RequestDelegateReturnValue_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_RequestDelegateReturnValue_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor RoutePatternIssue = new(
        "ASP0017",
        new LocalizableResourceString(nameof(Resources.Analyzer_RouteIssue_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_RouteIssue_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor RoutePatternUnusedParameter = new(
        "ASP0018",
        new LocalizableResourceString(nameof(Resources.Analyzer_UnusedParameter_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_UnusedParameter_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");
}
