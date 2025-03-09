// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class DiagnosticDescriptors
{
    private const string Encapsulation = "Encapsulation";
    private const string Usage = "Usage";

    private static LocalizableResourceString CreateLocalizableResourceString(string resource) => new(resource, Resources.ResourceManager, typeof(Resources));

    public static readonly DiagnosticDescriptor ComponentParameterSettersShouldBePublic = new(
        "BL0001",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Format)),
        Encapsulation,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Description)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesMustBeUnique = new(
        "BL0002",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Description)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesHasWrongType = new(
        "BL0003",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Description)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldBePublic = new(
        "BL0004",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Format)),
        Encapsulation,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParametersShouldBePublic_Description)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent = new(
        "BL0005",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Description)));

    public static readonly DiagnosticDescriptor DoNotUseRenderTreeTypes = new(
        "BL0006",
        CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Title)),
        CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldBeAutoProperties = new(
        "BL0007",
        CreateLocalizableResourceString(nameof(Resources.ComponentParametersShouldBeAutoProperties_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParametersShouldBeAutoProperties_Message)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
