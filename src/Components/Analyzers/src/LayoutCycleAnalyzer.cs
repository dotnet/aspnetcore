// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayoutCycleAnalyzer : DiagnosticAnalyzer
{
    public LayoutCycleAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(new[]
        {
            DiagnosticDescriptors.LayoutComponentCannotReferenceItself,
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

            // Check if LayoutAttribute and LayoutComponentBase are available
            if (symbols.LayoutAttribute == null || symbols.LayoutComponentBase == null)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                var namedType = (INamedTypeSymbol)context.Symbol;

                // Check if the type inherits from LayoutComponentBase (directly or indirectly)
                if (!InheritsFromLayoutComponentBase(namedType, symbols.LayoutComponentBase))
                {
                    return;
                }

                // Check if the type has a LayoutAttribute
                var layoutAttribute = namedType.GetAttributes()
                    .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, symbols.LayoutAttribute));

                if (layoutAttribute == null)
                {
                    return;
                }

                // Get the LayoutType from the attribute constructor argument
                if (layoutAttribute.ConstructorArguments.Length > 0)
                {
                    var layoutType = layoutAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    
                    // Check if the layout type is the same as the current type (self-reference)
                    if (layoutType != null && SymbolEqualityComparer.Default.Equals(namedType, layoutType))
                    {
                        var location = namedType.Locations.FirstOrDefault() ?? Location.None;
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.LayoutComponentCannotReferenceItself,
                            location,
                            namedType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
                    }
                }
            }, SymbolKind.NamedType);
        });
    }

    private static bool InheritsFromLayoutComponentBase(INamedTypeSymbol type, INamedTypeSymbol layoutComponentBase)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, layoutComponentBase))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}