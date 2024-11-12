// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnRouteHandlerParameters = new(
        "ASP0003",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseModelBindingAttributesOnRouteHandlerParameters_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseModelBindingAttributesOnRouteHandlerParameters_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotReturnActionResultsFromRouteHandlers = new(
        "ASP0004",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotReturnActionResultsFromRouteHandlers_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotReturnActionResultsFromRouteHandlers_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DetectMisplacedLambdaAttribute = new(
        "ASP0005",
        new LocalizableResourceString(nameof(Resources.Analyzer_DetectMisplacedLambdaAttribute_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DetectMisplacedLambdaAttribute_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseNonLiteralSequenceNumbers = new(
        "ASP0006",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseNonLiteralSequenceNumbers_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseNonLiteralSequenceNumbers_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DetectMismatchedParameterOptionality = new(
        "ASP0007",
        new LocalizableResourceString(nameof(Resources.Analyzer_DetectMismatchedParameterOptionality_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DetectMismatchedParameterOptionality_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWebHostWithConfigureHostBuilder = new(
        "ASP0008",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWebHostWithConfigureHostBuilder_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWebHostWithConfigureHostBuilder_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWithConfigureWebHostBuilder = new(
        "ASP0009",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWithConfigureWebHostBuilder_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWithConfigureWebHostBuilder_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseUseStartupWithConfigureWebHostBuilder = new(
        "ASP0010",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseUseStartupWithConfigureWebHostBuilder_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseUseStartupWithConfigureWebHostBuilder_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureLogging = new(
        "ASP0011",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureLogging_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureLogging_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureServices = new(
        "ASP0012",
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureServices_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureServices_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor DisallowConfigureAppConfigureHostBuilder = new(
        "ASP0013",
        new LocalizableResourceString(nameof(Resources.Analyzer_DisallowConfigureAppConfigureHostBuilder_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_DisallowConfigureAppConfigureHostBuilder_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor UseTopLevelRouteRegistrationsInsteadOfUseEndpoints = new(
        "ASP0014",
        new LocalizableResourceString(nameof(Resources.Analyzer_UseTopLevelRouteRegistrationsInsteadOfUseEndpoints_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_UseTopLevelRouteRegistrationsInsteadOfUseEndpoints_Message), Resources.ResourceManager, typeof(Resources)),
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

    internal static readonly DiagnosticDescriptor DoNotUseIHeaderDictionaryAdd = new(
        "ASP0019",
        new LocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryAdd_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryAdd_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor RouteParameterComplexTypeIsNotParsable = new(
        "ASP0020",
        new LocalizableResourceString(nameof(Resources.Analyzer_RouteParameterComplexTypeIsNotParsable_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_RouteParameterComplexTypeIsNotParsable_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor BindAsyncSignatureMustReturnValueTaskOfT = new(
        "ASP0021",
        new LocalizableResourceString(nameof(Resources.Analyzer_BindAsyncSignatureMustReturnValueTaskOfT_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_BindAsyncSignatureMustReturnValueTaskOfT_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor AmbiguousRouteHandlerRoute = new(
        "ASP0022",
        new LocalizableResourceString(nameof(Resources.Analyzer_AmbiguousRouteHandlerRoute_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_AmbiguousRouteHandlerRoute_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor AmbiguousActionRoute = new(
        "ASP0023",
        new LocalizableResourceString(nameof(Resources.Analyzer_AmbiguousActionRoute_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_AmbiguousActionRoute_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor AtMostOneFromBodyAttribute = new(
        "ASP0024",
        new LocalizableResourceString(nameof(Resources.Analyzer_MultipleFromBody_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_MultipleFromBody_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor UseAddAuthorizationBuilder = new(
        "ASP0025",
        new LocalizableResourceString(nameof(Resources.Analyzer_UseAddAuthorizationBuilder_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_UseAddAuthorizationBuilder_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor OverriddenAuthorizeAttribute = new(
        "ASP0026",
        new LocalizableResourceString(nameof(Resources.Analyzer_OverriddenAuthorizeAttribute_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_OverriddenAuthorizeAttribute_Message), Resources.ResourceManager, typeof(Resources)),
        "Security",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");

    internal static readonly DiagnosticDescriptor PublicPartialProgramClassNotRequired = new(
        "ASP0027",
        new LocalizableResourceString(nameof(Resources.Analyzer_PublicPartialProgramClass_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_PublicPartialProgramClass_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers",
        customTags: WellKnownDiagnosticTags.Unnecessary);

    internal static readonly DiagnosticDescriptor KestrelShouldListenOnIPv6AnyInsteadOfIpAny = new(
        "ASP0028",
        new LocalizableResourceString(nameof(Resources.Analyzer_KestrelShouldListenOnIPv6AnyInsteadOfIpAny_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.Analyzer_KestrelShouldListenOnIPv6AnyInsteadOfIpAny_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://aka.ms/aspnet/analyzers");
}
