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

    // Note: The Razor Compiler (including Components features) use the RZ prefix for diagnostics, so there's currently
    // no change of clashing between that and the BL prefix used here.
    //
    // Tracking https://github.com/dotnet/aspnetcore/issues/10382 to rationalize this
    public static readonly DiagnosticDescriptor ComponentParameterSettersShouldBePublic = new DiagnosticDescriptor(
        "BL0001",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Format)),
        Encapsulation,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Description)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesMustBeUnique = new DiagnosticDescriptor(
        "BL0002",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Description)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesHasWrongType = new DiagnosticDescriptor(
        "BL0003",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Description)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldBePublic = new DiagnosticDescriptor(
        "BL0004",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Format)),
        Encapsulation,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParametersShouldBePublic_Description)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent = new DiagnosticDescriptor(
        "BL0005",
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Title)),
        CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Format)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Description)));

    public static readonly DiagnosticDescriptor DoNotUseRenderTreeTypes = new DiagnosticDescriptor(
        "BL0006",
        CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Title)),
        CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description)),
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description)));
}
