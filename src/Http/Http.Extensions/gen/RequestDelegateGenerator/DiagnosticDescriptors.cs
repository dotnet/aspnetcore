// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

internal static class DiagnosticDescriptors
{
    private static string GetHelpLinkUrl(string id) => $"https://learn.microsoft.com/aspnet/core/fundamentals/aot/request-delegate-generator/diagnostics/{id}";

    public static DiagnosticDescriptor UnableToResolveRoutePattern { get; } = new(
        "RDG001",
        new LocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveRoutePattern_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG001"));

    public static DiagnosticDescriptor UnableToResolveMethod { get; } = new(
        "RDG002",
        new LocalizableResourceString(nameof(Resources.UnableToResolveMethod_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveMethod_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG002"));

    public static DiagnosticDescriptor UnableToResolveParameterDescriptor { get; } = new(
        "RDG003",
        new LocalizableResourceString(nameof(Resources.UnableToResolveParameter_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveParameter_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG003"));

    public static DiagnosticDescriptor UnableToResolveAnonymousReturnType { get; } = new(
        "RDG004",
        new LocalizableResourceString(nameof(Resources.UnableToResolveAnonymousReturnType_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.UnableToResolveAnonymousReturnType_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG004"));

    public static DiagnosticDescriptor InvalidAsParametersAbstractType { get; } = new(
        "RDG005",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersAbstractType_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersAbstractType_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG005"));

    public static DiagnosticDescriptor InvalidAsParametersSignature { get; } = new(
        "RDG006",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersSignature_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersSignature_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG006"));

    public static DiagnosticDescriptor InvalidAsParametersNoConstructorFound { get; } = new(
        "RDG007",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNoConstructorFound_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNoConstructorFound_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG007"));

    public static DiagnosticDescriptor InvalidAsParametersSingleConstructorOnly { get; } = new(
        "RDG008",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersSingleConstructorOnly_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersSingleConstructorOnly_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG008"));

    public static DiagnosticDescriptor InvalidAsParametersNested { get; } = new(
        "RDG009",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNested_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNested_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG009"));

    public static DiagnosticDescriptor InvalidAsParametersNullable { get; } = new(
        "RDG010",
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNullable_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InvalidAsParametersNullable_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG010"));

    public static DiagnosticDescriptor TypeParametersNotSupported { get; } = new(
        "RDG011",
        new LocalizableResourceString(nameof(Resources.TypeParametersNotSupported_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.TypeParametersNotSupported_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG011"));

    public static DiagnosticDescriptor InaccessibleTypesNotSupported { get; } = new(
        "RDG012",
        new LocalizableResourceString(nameof(Resources.InaccessibleTypesNotSupported_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.InaccessibleTypesNotSupported_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG012"));

    public static DiagnosticDescriptor KeyedAndNotKeyedServiceAttributesNotSupported { get; } = new(
        "RDG013",
        new LocalizableResourceString(nameof(Resources.KeyedAndNotKeyedServiceAttributesNotSupported_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.KeyedAndNotKeyedServiceAttributesNotSupported_Message), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: GetHelpLinkUrl("RDG013"));
}
