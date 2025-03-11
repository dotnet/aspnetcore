// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

internal static class DiagnosticDescriptors
{
    private const string Usage = "Usage";

    private static LocalizableResourceString CreateLocalizableResourceString(string resource) => new(resource, Resources.ResourceManager, typeof(Resources));
    private static string GetHelpLinkUrl(string id) => $"https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/diagnostics/{id}";

    public static DiagnosticDescriptor UnableToResolveRoutePattern { get; } = new(
        "RDG001",
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Title)),
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG001"));

    public static DiagnosticDescriptor UnableToResolveMethod { get; } = new(
        "RDG002",
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveMethod_Title)),
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveMethod_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG002"));

    public static DiagnosticDescriptor UnableToResolveParameterDescriptor { get; } = new(
        "RDG003",
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveParameter_Title)),
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveParameter_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG003"));

    public static DiagnosticDescriptor UnableToResolveAnonymousReturnType { get; } = new(
        "RDG004",
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveAnonymousReturnType_Title)),
        CreateLocalizableResourceString(nameof(Resources.UnableToResolveAnonymousReturnType_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG004"));

    public static DiagnosticDescriptor InvalidAsParametersAbstractType { get; } = new(
        "RDG005",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersAbstractType_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersAbstractType_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG005"));

    public static DiagnosticDescriptor InvalidAsParametersSignature { get; } = new(
        "RDG006",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersSignature_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersSignature_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG006"));

    public static DiagnosticDescriptor InvalidAsParametersNoConstructorFound { get; } = new(
        "RDG007",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNoConstructorFound_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNoConstructorFound_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG007"));

    public static DiagnosticDescriptor InvalidAsParametersSingleConstructorOnly { get; } = new(
        "RDG008",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersSingleConstructorOnly_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersSingleConstructorOnly_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG008"));

    public static DiagnosticDescriptor InvalidAsParametersNested { get; } = new(
        "RDG009",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNested_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNested_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG009"));

    public static DiagnosticDescriptor InvalidAsParametersNullable { get; } = new(
        "RDG010",
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNullable_Title)),
        CreateLocalizableResourceString(nameof(Resources.InvalidAsParametersNullable_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG010"));

    public static DiagnosticDescriptor TypeParametersNotSupported { get; } = new(
        "RDG011",
        CreateLocalizableResourceString(nameof(Resources.TypeParametersNotSupported_Title)),
        CreateLocalizableResourceString(nameof(Resources.TypeParametersNotSupported_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG011"));

    public static DiagnosticDescriptor InaccessibleTypesNotSupported { get; } = new(
        "RDG012",
        CreateLocalizableResourceString(nameof(Resources.InaccessibleTypesNotSupported_Title)),
        CreateLocalizableResourceString(nameof(Resources.InaccessibleTypesNotSupported_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG012"));

    public static DiagnosticDescriptor KeyedAndNotKeyedServiceAttributesNotSupported { get; } = new(
        "RDG013",
        CreateLocalizableResourceString(nameof(Resources.KeyedAndNotKeyedServiceAttributesNotSupported_Title)),
        CreateLocalizableResourceString(nameof(Resources.KeyedAndNotKeyedServiceAttributesNotSupported_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG013"));

    public static DiagnosticDescriptor InvalidAsParametersEnumerableType { get; } = new(
        "RDG014",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersEnumerableType_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersEnumerableType_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG014"));
}
