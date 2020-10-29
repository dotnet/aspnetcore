// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class RazorDiagnostics
    {
        public static readonly DiagnosticDescriptor InvalidRazorLangVersionDescriptor = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "RZ3600",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Invalid RazorLangVersion",
            "Invalid value {0} for RazorLangVersion. Valid values include 'Latest' or a valid version in range 1.0 to 5.0.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static Diagnostic AsDiagnostic(this RazorDiagnostic razorDiagnostic)
        {
            var descriptor = new DiagnosticDescriptor(
                razorDiagnostic.Id,
                razorDiagnostic.GetMessage(CultureInfo.CurrentCulture),
                razorDiagnostic.GetMessage(CultureInfo.CurrentCulture),
                "Razor",
                razorDiagnostic.Severity switch
                {
                    RazorDiagnosticSeverity.Error => DiagnosticSeverity.Error,
                    RazorDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                    _ => DiagnosticSeverity.Hidden,
                },
                isEnabledByDefault: true);

            var span = razorDiagnostic.Span;
            var location = Location.Create(
                span.FilePath,
                span.AsTextSpan(),
                new LinePositionSpan(
                    new LinePosition(span.LineIndex, span.CharacterIndex),
                    new LinePosition(span.LineIndex, span.CharacterIndex + span.Length)));

            return Diagnostic.Create(descriptor, location);
        }
    }
}
