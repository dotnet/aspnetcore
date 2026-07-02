// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that reports a warning when InputSelect&lt;T&gt; is used with a nullable type
/// without providing an empty &lt;option&gt; to represent null.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InputSelectAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(DiagnosticDescriptors.InputSelectRequiresEmptyOption);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    /// <summary>
    /// Analyzes a named type and reports diagnostics for nullable InputSelect usage.
    /// </summary>
    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var containingType = (INamedTypeSymbol)context.Symbol;

        foreach (var property in containingType.GetMembers().OfType<IPropertySymbol>())
        {
            if (!TryGetInputSelectTypeArgument(property, out var typeArgument, out var typeArgumentSyntax))
            {
                continue;
            }

            if (!IsNullableType(typeArgument, context.Compilation, typeArgumentSyntax))
            {
                continue;
            }

            if (HasEmptyOption(property))
            {
                continue;
            }

            var location = typeArgumentSyntax?.GetLocation() ?? property.Locations.FirstOrDefault();
            if (location is null)
            {
                continue;
            }

            var displayText = GetFormattedTypeName(typeArgument);

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InputSelectRequiresEmptyOption,
                location,
                displayText));
        }
    }

    /// <summary>
    /// Attempts to extract the type argument from an InputSelect&lt;T&gt; property.
    /// </summary>
    private static bool TryGetInputSelectTypeArgument(
        IPropertySymbol property,
        out ITypeSymbol typeArgument,
        out TypeSyntax? typeArgumentSyntax)
    {
        typeArgument = null!;
        typeArgumentSyntax = null;

        if (property.Type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var original = namedType.OriginalDefinition;
        if (original.Name != "InputSelect" || original.Arity != 1)
        {
            return false;
        }

        typeArgument = namedType.TypeArguments[0];

        // Extract syntax from property declaration
        var syntaxRef = property.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef?.GetSyntax() is PropertyDeclarationSyntax propertySyntax)
        {
            var propertyType = propertySyntax.Type;

            // Handle direct generic name: InputSelect<T>
            if (propertyType is GenericNameSyntax generic &&
                generic.TypeArgumentList.Arguments.Count == 1)
            {
                typeArgumentSyntax = generic.TypeArgumentList.Arguments[0];
            }
            // Handle qualified generic name: namespace.InputSelect<T>
            else if (propertyType is QualifiedNameSyntax qualified &&
                qualified.Right is GenericNameSyntax qualifiedGeneric &&
                qualifiedGeneric.TypeArgumentList.Arguments.Count == 1)
            {
                typeArgumentSyntax = qualifiedGeneric.TypeArgumentList.Arguments[0];
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the given type represents a nullable type.
    /// </summary>
    private static bool IsNullableType(
        ITypeSymbol type,
        Compilation compilation,
        TypeSyntax? typeSyntax)
    {
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        if (typeSyntax is NullableTypeSyntax)
        {
            return true;
        }

        if (typeSyntax is not null && typeSyntax.ToString().Contains("?", StringComparison.Ordinal))
        {
            return true;
        }

        if (typeSyntax is null && compilation.Options.NullableContextOptions == NullableContextOptions.Disable)
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Formats the type name to match diagnostic expectations.
    /// </summary>
    private static string GetFormattedTypeName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var innerType = named.TypeArguments[0];

            if (innerType.TypeKind == TypeKind.Enum)
            {
                return innerType.Name;
            }

            var innerDisplay = innerType.ToDisplayString(FullyQualifiedTypeFormat);
            return $"System.Nullable<{innerDisplay}>";
        }

        return type.ToDisplayString(FullyQualifiedTypeFormat);
    }

    /// <summary>
    /// Checks if an empty option (&lt;option value=""&gt;) exists.
    /// </summary>
    private static bool HasEmptyOption(IPropertySymbol property)
    {
        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            var node = syntaxRef.GetSyntax();
            var text = node.ToString();

            if (text.Contains("<option", System.StringComparison.OrdinalIgnoreCase) &&
                (text.Contains("value=\"\"", System.StringComparison.OrdinalIgnoreCase) ||
                text.Contains("value=''", System.StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
