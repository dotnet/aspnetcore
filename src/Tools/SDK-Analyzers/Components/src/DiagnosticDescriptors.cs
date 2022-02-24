// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
internal static class DiagnosticDescriptors
{
    // Note: The Razor Compiler (including Components features) use the RZ prefix for diagnostics, so there's currently
    // no change of clashing between that and the BL prefix used here.
    //
    // Tracking https://github.com/dotnet/aspnetcore/issues/10382 to rationalize this
    public static readonly DiagnosticDescriptor ComponentParameterSettersShouldBePublic = new DiagnosticDescriptor(
        "BL0001",
        new LocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Format), Resources.ResourceManager, typeof(Resources)),
        "Encapsulation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.ComponentParameterSettersShouldBePublic_Description), Resources.ResourceManager, typeof(Resources)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesMustBeUnique = new DiagnosticDescriptor(
        "BL0002",
        new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Format), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesMustBeUnique_Description), Resources.ResourceManager, typeof(Resources)));

    public static readonly DiagnosticDescriptor ComponentParameterCaptureUnmatchedValuesHasWrongType = new DiagnosticDescriptor(
        "BL0003",
        new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Format), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.ComponentParameterCaptureUnmatchedValuesHasWrongType_Description), Resources.ResourceManager, typeof(Resources)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldBePublic = new DiagnosticDescriptor(
        "BL0004",
        new LocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.ComponentParameterShouldBePublic_Format), Resources.ResourceManager, typeof(Resources)),
        "Encapsulation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.ComponentParametersShouldBePublic_Description), Resources.ResourceManager, typeof(Resources)));

    public static readonly DiagnosticDescriptor ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent = new DiagnosticDescriptor(
        "BL0005",
        new LocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Format), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.ComponentParameterShouldNotBeSetOutsideOfTheirDeclaredComponent_Description), Resources.ResourceManager, typeof(Resources)));

    public static readonly DiagnosticDescriptor DoNotUseRenderTreeTypes = new DiagnosticDescriptor(
        "BL0006",
        new LocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description), Resources.ResourceManager, typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.DoNotUseRenderTreeTypes_Description), Resources.ResourceManager, typeof(Resources)));
}
