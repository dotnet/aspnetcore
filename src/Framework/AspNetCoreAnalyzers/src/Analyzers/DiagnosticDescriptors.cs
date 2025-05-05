// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class DiagnosticDescriptors
{
    private const string Security = "Security";
    private const string Usage = "Usage";
    private const string AnalyzersLink = "https://aka.ms/aspnet/analyzers";

    private static LocalizableResourceString CreateLocalizableResourceString(string resource) => new(resource, Resources.ResourceManager, typeof(Resources));
    
    internal static readonly DiagnosticDescriptor DoNotUseModelBindingAttributesOnRouteHandlerParameters = new(
        "ASP0003",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseModelBindingAttributesOnRouteHandlerParameters_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseModelBindingAttributesOnRouteHandlerParameters_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotReturnActionResultsFromRouteHandlers = new(
        "ASP0004",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotReturnActionResultsFromRouteHandlers_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotReturnActionResultsFromRouteHandlers_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DetectMisplacedLambdaAttribute = new(
        "ASP0005",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DetectMisplacedLambdaAttribute_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DetectMisplacedLambdaAttribute_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseNonLiteralSequenceNumbers = new(
        "ASP0006",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseNonLiteralSequenceNumbers_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseNonLiteralSequenceNumbers_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DetectMismatchedParameterOptionality = new(
        "ASP0007",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DetectMismatchedParameterOptionality_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DetectMismatchedParameterOptionality_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWebHostWithConfigureHostBuilder = new(
        "ASP0008",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWebHostWithConfigureHostBuilder_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWebHostWithConfigureHostBuilder_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseConfigureWithConfigureWebHostBuilder = new(
        "ASP0009",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWithConfigureWebHostBuilder_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseConfigureWithConfigureWebHostBuilder_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseUseStartupWithConfigureWebHostBuilder = new(
        "ASP0010",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseUseStartupWithConfigureWebHostBuilder_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseUseStartupWithConfigureWebHostBuilder_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureLogging = new(
        "ASP0011",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureLogging_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureLogging_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseHostConfigureServices = new(
        "ASP0012",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureServices_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DoNotUseHostConfigureServices_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DisallowConfigureAppConfigureHostBuilder = new(
        "ASP0013",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DisallowConfigureAppConfigureHostBuilder_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_DisallowConfigureAppConfigureHostBuilder_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor UseTopLevelRouteRegistrationsInsteadOfUseEndpoints = new(
        "ASP0014",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UseTopLevelRouteRegistrationsInsteadOfUseEndpoints_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UseTopLevelRouteRegistrationsInsteadOfUseEndpoints_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor UseHeaderDictionaryPropertiesInsteadOfIndexer = new(
        "ASP0015",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryIndexer_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryIndexer_Message)),
        Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotReturnValueFromRequestDelegate = new(
        "ASP0016",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RequestDelegateReturnValue_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RequestDelegateReturnValue_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor RoutePatternIssue = new(
        "ASP0017",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RouteIssue_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RouteIssue_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor RoutePatternUnusedParameter = new(
        "ASP0018",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UnusedParameter_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UnusedParameter_Message)),
        Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor DoNotUseIHeaderDictionaryAdd = new(
        "ASP0019",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryAdd_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_HeaderDictionaryAdd_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor RouteParameterComplexTypeIsNotParsable = new(
        "ASP0020",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RouteParameterComplexTypeIsNotParsable_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_RouteParameterComplexTypeIsNotParsable_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor BindAsyncSignatureMustReturnValueTaskOfT = new(
        "ASP0021",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_BindAsyncSignatureMustReturnValueTaskOfT_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_BindAsyncSignatureMustReturnValueTaskOfT_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor AmbiguousRouteHandlerRoute = new(
        "ASP0022",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_AmbiguousRouteHandlerRoute_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_AmbiguousRouteHandlerRoute_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor AmbiguousActionRoute = new(
        "ASP0023",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_AmbiguousActionRoute_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_AmbiguousActionRoute_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor AtMostOneFromBodyAttribute = new(
        "ASP0024",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_MultipleFromBody_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_MultipleFromBody_Message)),
        Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor UseAddAuthorizationBuilder = new(
        "ASP0025",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UseAddAuthorizationBuilder_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_UseAddAuthorizationBuilder_Message)),
        Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor OverriddenAuthorizeAttribute = new(
        "ASP0026",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_OverriddenAuthorizeAttribute_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_OverriddenAuthorizeAttribute_Message)),
        Security,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);

    internal static readonly DiagnosticDescriptor PublicPartialProgramClassNotRequired = new(
        "ASP0027",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_PublicPartialProgramClass_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_PublicPartialProgramClass_Message)),
        Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink,
        customTags: WellKnownDiagnosticTags.Unnecessary);

    internal static readonly DiagnosticDescriptor KestrelShouldListenOnIPv6AnyInsteadOfIpAny = new(
        "ASP0028",
        CreateLocalizableResourceString(nameof(Resources.Analyzer_KestrelShouldListenOnIPv6AnyInsteadOfIpAny_Title)),
        CreateLocalizableResourceString(nameof(Resources.Analyzer_KestrelShouldListenOnIPv6AnyInsteadOfIpAny_Message)),
        Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzersLink);
}
