// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InputSelectAnalyzer : DiagnosticAnalyzer
{
    public InputSelectAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.InputSelectRequiresEmptyOption);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    /// Initializes the analyzer to detect when InputSelect components use nullable types without an empty option.
    /// </summary>
    /// <param name="context">The analysis context where diagnostics are reported.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var componentSymbolsResult = ComponentSymbols.TryCreate(context.Compilation, out var symbols);
            if (!componentSymbolsResult)
            {
                symbols = null;
            }

            var inputSelectType = context.Compilation.GetTypeByMetadataName(ComponentsApi.InputSelect.MetadataName);

            context.RegisterSymbolStartAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;

                // Only analyze classes that inherit from ComponentBase (if symbols available)
                if (symbols != null && symbols.ComponentBaseType != null && !ComponentFacts.IsComponentBase(symbols, type))
                {
                    return;
                }

                // Find properties that use InputSelect<TValue>
                var inputSelectProperties = type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => IsInputSelectProperty(p, inputSelectType))
                    .ToList();

                if (inputSelectProperties.Count == 0)
                {
                    return;
                }

                context.RegisterSymbolEndAction(context =>
                {
                    foreach (var property in inputSelectProperties)
                    {
                        var location = property.Locations.FirstOrDefault();
                        if (location == null)
                        {
                            continue;
                        }

                        // Check if TValue is nullable
                        var valueType = property.Type;
                        if (valueType is INamedTypeSymbol genericType &&
                            genericType.TypeArguments.Length == 1 &&
                            IsNullableType(genericType.TypeArguments[0]))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.InputSelectRequiresEmptyOption,
                                location,
                                valueType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }
                    }
                });
            }, SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Determines whether a property is an InputSelect component with a type parameter.
    /// </summary>
    /// <param name="property">The property symbol to check.</param>
    /// <param name="inputSelectType">The InputSelect type symbol to match against.</param>
    /// <returns>True if the property is an InputSelect component; otherwise, false.</returns>
    private static bool IsInputSelectProperty(IPropertySymbol property, ITypeSymbol inputSelectType)
    {
        var propertyType = property.Type;

        // Check for unresolved or error type cases first (handles test declarations without using statements)
        if (propertyType.TypeKind == TypeKind.Error)
        {
            var name = propertyType.Name;
            if (name == "InputSelect" || name.StartsWith("InputSelect<", StringComparison.Ordinal) || name == "InputSelect`1")
            {
                return true;
            }
        }

        // Check if property type is a named generic type
        if (propertyType is INamedTypeSymbol namedType)
        {
            // Case 1: Direct InputSelect<TValue> from Forms namespace with matching original definition
            if (inputSelectType != null &&
                namedType.OriginalDefinition != null &&
                SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, inputSelectType))
            {
                return true;
            }

            // Case 2: InputSelect<TValue> identified by name (handles both metadata names and simple names)
            // This covers both constructed types like InputSelect<int?> and their original definitions
            if (namedType.Name == "InputSelect" || namedType.MetadataName == "InputSelect`1")
            {
                return true;
            }

            // Case 3: Check the original definition's name
            var originalDef = namedType.OriginalDefinition;
            if (originalDef != null && (originalDef.Name == "InputSelect" || originalDef.MetadataName == "InputSelect`1"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a type is nullable, including both Nullable&lt;T&gt; value types and nullable reference types.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>True if the type is nullable; otherwise, false.</returns>
    private static bool IsNullableType(ITypeSymbol type)
    {
        if (type == null)
        {
            return false;
        }

        // Handle Nullable<T> (int?, DateTime?, etc.)
        if (type is INamedTypeSymbol namedType &&
            namedType.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        // Handle nullable reference types using display format
        var display = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        if (display.EndsWith("?", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
