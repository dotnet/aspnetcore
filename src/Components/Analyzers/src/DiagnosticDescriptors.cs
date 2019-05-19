// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    internal static class DiagnosticDescriptors
    {
        // Note: The Razor Compiler (including Components features) use the RZ prefix for diagnostics, so there's currently
        // no change of clashing between that and the BL prefix used here.
        //
        // Tracking https://github.com/aspnet/AspNetCore/issues/10382 to rationalize this
        public static readonly DiagnosticDescriptor ComponentParametersShouldNotBePublic = new DiagnosticDescriptor(
            "BL0001",
            new LocalizableResourceString(nameof(Resources.ComponentParametersShouldNotBePublic_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ComponentParametersShouldNotBePublic_Format), Resources.ResourceManager, typeof(Resources)),
            "Encapsulation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.ComponentParametersShouldNotBePublic_Description), Resources.ResourceManager, typeof(Resources)));

        public static readonly DiagnosticDescriptor ComponentCaptureExtraAttributesParameterMustBeUnique = new DiagnosticDescriptor(
            "BL0002",
            new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterMustBeUnique_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterMustBeUnique_Format), Resources.ResourceManager, typeof(Resources)),
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterMustBeUnique_Description), Resources.ResourceManager, typeof(Resources)));

        public static readonly DiagnosticDescriptor ComponentCaptureExtraAttributesParameterHasWrongType = new DiagnosticDescriptor(
            "BL0003",
            new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterHasWrongType_Title), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterHasWrongType_Format), Resources.ResourceManager, typeof(Resources)),
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.ComponentCaptureExtraAttributesParameterHasWrongType_Description), Resources.ResourceManager, typeof(Resources)));
    }
}
