// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal static bool ShouldTransformSymbolWithAttribute(SyntaxNode syntaxNode, CancellationToken _)
    {
        return syntaxNode is ClassDeclarationSyntax or RecordDeclarationSyntax;
    }

    internal ImmutableArray<ValidatableType> TransformValidatableTypeWithAttribute(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);
        if (TryExtractValidatableType((ITypeSymbol)context.TargetSymbol, wellKnownTypes, ref validatableTypes, ref visitedTypes))
        {
            return [..validatableTypes];
        }
        return [];
    }

    internal static bool ShouldTransformSymbolWithValidatableTypeAttribute(SyntaxNode syntaxNode, CancellationToken _)
    {
        // Only process class and record declarations
        if (syntaxNode is not (ClassDeclarationSyntax or RecordDeclarationSyntax))
        {
            return false;
        }

        // Check if the type has any attribute that could be ValidatableTypeAttribute
        var typeDeclaration = (TypeDeclarationSyntax)syntaxNode;
        return typeDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(IsValidatableTypeAttribute);
    }

    internal ImmutableArray<ValidatableType> TransformValidatableTypeWithValidatableTypeAttribute(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not (ClassDeclarationSyntax or RecordDeclarationSyntax))
        {
            return [];
        }

        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) as ITypeSymbol;
        if (typeSymbol == null)
        {
            return [];
        }

        // Check if the type has a ValidatableTypeAttribute (framework or auto-generated)
        if (!HasValidatableTypeAttribute(typeSymbol))
        {
            return [];
        }

        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);

        if (TryExtractValidatableType(typeSymbol, wellKnownTypes, ref validatableTypes, ref visitedTypes))
        {
            return [..validatableTypes];
        }
        return [];
    }

    private static bool IsValidatableTypeAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name is "ValidatableType" or "ValidatableTypeAttribute" ||
               name.EndsWith(".ValidatableType", StringComparison.Ordinal) ||
               name.EndsWith(".ValidatableTypeAttribute", StringComparison.Ordinal);
    }

    private static bool HasValidatableTypeAttribute(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(attr =>
        {
            var attributeClass = attr.AttributeClass;
            if (attributeClass == null)
            {
                return false;
            }

            var name = attributeClass.Name;
            var fullName = attributeClass.ToDisplayString();

            // Check for framework attribute
            if (fullName == "Microsoft.Extensions.Validation.ValidatableTypeAttribute")
            {
                return true;
            }

            // Check for auto-generated attribute (any namespace)
            if (name == "ValidatableTypeAttribute")
            {
                // Additional check: ensure it's marked with [Embedded] to confirm it's auto-generated
                return attributeClass.GetAttributes().Any(embeddedAttr =>
                    embeddedAttr.AttributeClass?.ToDisplayString() == "Microsoft.CodeAnalysis.EmbeddedAttribute");
            }

            return false;
        });
    }
}
