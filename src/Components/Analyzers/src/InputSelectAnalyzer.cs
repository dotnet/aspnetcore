// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.InputSelectRequiresEmptyOption);

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
        if (context.Symbol is not INamedTypeSymbol containingType)
        {
            return;
        }

        foreach (var property in containingType.GetMembers().OfType<IPropertySymbol>())
        {
            if (!TryGetInputSelectTypeArgument(property, out var typeArgument, out var typeArgumentSyntax))
            {
                continue;
            }

            if (!IsNullableType(typeArgument, typeArgumentSyntax))
            {
                continue;
            }

            // Skip diagnostic if empty <option value=""> exists
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

        if (property.Type is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        var original = namedTypeSymbol.OriginalDefinition;

        if (original.Name != "InputSelect" || original.Arity != 1)
        {
            return false;
        }

        typeArgument = namedTypeSymbol.TypeArguments[0];

        var syntaxReference = property.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax() is not PropertyDeclarationSyntax propertySyntax)
        {
            return true;
        }

        if (propertySyntax.Type is GenericNameSyntax generic &&
            generic.TypeArgumentList.Arguments.Count == 1)
        {
            typeArgumentSyntax = generic.TypeArgumentList.Arguments[0];
        }

        return true;
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

            // Special handling for enums
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
    /// Determines whether the given type represents a nullable type.
    /// </summary>
    private static bool IsNullableType(ITypeSymbol type, SyntaxNode? syntax = null)
    {
        // Nullable value type (Nullable&lt;T&gt;)
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        // Nullable reference type (T?)
        return syntax is NullableTypeSyntax;
    }

    /// <summary>
    /// Checks if an empty option (&lt;option value=""&gt;) exists.
    /// </summary>
    private static bool HasEmptyOption(IPropertySymbol property)
    {
        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            var syntaxText = syntax.ToString();

            if (syntaxText.Contains("option value=\"\""))
            {
                return true;
            }
        }

        return false;
    }
}
