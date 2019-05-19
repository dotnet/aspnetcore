// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComponentParametersShouldNotBePublicAnalyzer : DiagnosticAnalyzer
    {
        public ComponentParametersShouldNotBePublicAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(DiagnosticDescriptors.ComponentParametersShouldNotBePublic);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
                {
                    // Types we need are not defined.
                    return;
                }

                context.RegisterSymbolAction(context =>
                {
                    var property = (IPropertySymbol)context.Symbol;
                    if (!ComponentFacts.IsAnyParameter(symbols, property))
                    {
                        // Not annotated with [Parameter] or [CascadingParameter]
                        return;
                    }

                    if (property.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentParametersShouldNotBePublic,
                            property.Locations[0],
                            property.Name));
                    }
                }, SymbolKind.Property);
            });
        }
    }
}
