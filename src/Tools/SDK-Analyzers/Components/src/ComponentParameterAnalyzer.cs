// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComponentParameterAnalyzer : DiagnosticAnalyzer
{
    public ComponentParameterAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(new[]
        {
            DiagnosticDescriptors.ComponentParametersShouldBePublic,
            DiagnosticDescriptors.ComponentParameterSettersShouldBePublic,
            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesMustBeUnique,
            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesHasWrongType,
        });
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
            {
                // Types we need are not defined.
                return;
            }

            // This operates per-type because one of the validations we need has to look for duplicates
            // defined on the same type.
            context.RegisterSymbolStartAction(context =>
            {
                var properties = new List<IPropertySymbol>();

                var type = (INamedTypeSymbol)context.Symbol;
                foreach (var member in type.GetMembers())
                {
                    if (member is IPropertySymbol property && ComponentFacts.IsParameter(symbols, property))
                    {
                        // Annotated with [Parameter]. We ignore [CascadingParameter]'s because they don't interact with tooling and don't currently have any analyzer restrictions.
                        properties.Add(property);
                    }
                }

                if (properties.Count == 0)
                {
                    return;
                }

                context.RegisterSymbolEndAction(context =>
                {
                    var captureUnmatchedValuesParameters = new List<IPropertySymbol>();

                    // Per-property validations
                    foreach (var property in properties)
                    {
                        var propertyLocation = property.Locations.FirstOrDefault();
                        if (propertyLocation == null)
                        {
                            continue;
                        }

                        if (property.DeclaredAccessibility != Accessibility.Public)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ComponentParametersShouldBePublic,
                                propertyLocation,
                                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }
                        else if (property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ComponentParameterSettersShouldBePublic,
                                propertyLocation,
                                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }

                        if (ComponentFacts.IsParameterWithCaptureUnmatchedValues(symbols, property))
                        {
                            captureUnmatchedValuesParameters.Add(property);

                            // Check the type, we need to be able to assign a Dictionary<string, object>
                            var conversion = context.Compilation.ClassifyConversion(symbols.ParameterCaptureUnmatchedValuesRuntimeType, property.Type);
                            if (!conversion.Exists || conversion.IsExplicit)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesHasWrongType,
                                    propertyLocation,
                                    property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    property.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    symbols.ParameterCaptureUnmatchedValuesRuntimeType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                            }
                        }
                    }

                    // Check if the type defines multiple CaptureUnmatchedValues parameters. Doing this outside the loop means we place the
                    // errors on the type.
                    if (captureUnmatchedValuesParameters.Count > 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesMustBeUnique,
                            context.Symbol.Locations[0],
                            type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                            Environment.NewLine,
                            string.Join(
                                Environment.NewLine,
                                captureUnmatchedValuesParameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)).OrderBy(n => n))));
                    }
                });
            }, SymbolKind.NamedType);
        });
    }
}
