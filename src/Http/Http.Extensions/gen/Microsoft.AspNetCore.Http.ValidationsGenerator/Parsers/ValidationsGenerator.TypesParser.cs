// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat _symbolDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    internal ImmutableArray<ValidatableType> ExtractValidatableTypes(IInvocationOperation operation, RequiredSymbols requiredSymbols)
    {
        AnalyzerDebug.Assert(operation.SemanticModel != null, "SemanticModel should not be null.");
        var parameters = operation.TryGetRouteHandlerMethod(operation.SemanticModel, out var method)
            ? method.Parameters
            : [];
        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        foreach (var parameter in parameters)
        {
            _ = TryExtractValidatableType(parameter.Type.UnwrapType(requiredSymbols.IEnumerable), requiredSymbols, ref validatableTypes, ref visitedTypes);
        }
        return [.. validatableTypes];
    }

    internal bool TryExtractValidatableType(ITypeSymbol typeSymbol, RequiredSymbols requiredSymbols, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
    {
        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return false;
        }

        if (visitedTypes.Contains(typeSymbol))
        {
            return true;
        }

        visitedTypes.Add(typeSymbol);

        // Extract validatable types discovered in base types of this type and add them to the top-level list.
        var current = typeSymbol.BaseType;
        List<string>? validatableSubTypes = current != null && current.SpecialType != SpecialType.System_Object
            ? []
            : null;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            _ = TryExtractValidatableType(current, requiredSymbols, ref validatableTypes, ref visitedTypes);
            validatableSubTypes?.Add(current.Name);
            current = current.BaseType;
        }

        // Extract validatable types discovered in members of this type and add them to the top-level list.
        var members = ExtractValidatableMembers(typeSymbol, requiredSymbols, ref validatableTypes, ref visitedTypes);

        // Extract the validatable types discovered in the JsonDerivedTypeAttributes of this type and add them to the top-level list.
        var derivedTypes = typeSymbol.GetJsonDerivedTypes(requiredSymbols.JsonDerivedTypeAttribute);
        var derivedTypeNames = derivedTypes?.Select(t => t.ToDisplayString(_symbolDisplayFormat)).ToArray() ?? [];
        foreach (var derivedType in derivedTypes ?? [])
        {
            _ = TryExtractValidatableType(derivedType, requiredSymbols, ref validatableTypes, ref visitedTypes);
        }

        // Add the type itself as a validatable type itself.
        validatableTypes.Add(new ValidatableType(
            Type: typeSymbol,
            Members: members,
            IsIValidatableObject: typeSymbol.ImplementsInterface(requiredSymbols.IValidatableObject),
            ValidatableSubTypeNames: validatableSubTypes?.Count > 0 ? [.. validatableSubTypes] : ImmutableArray<string>.Empty,
            ValidatableDerivedTypeNames: [.. derivedTypeNames]));

        return true;
    }

    internal ImmutableArray<ValidatableProperty> ExtractValidatableMembers(ITypeSymbol typeSymbol, RequiredSymbols requiredSymbols, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
    {
        var members = new List<ValidatableProperty>();
        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var hasValidatableType = TryExtractValidatableType(member.Type.UnwrapType(requiredSymbols.IEnumerable), requiredSymbols, ref validatableTypes, ref visitedTypes);
            var attributes = ExtractValidationAttributes(member, requiredSymbols, out var isRequired);
            members.Add(new ValidatableProperty(
                ContainingType: member.ContainingType,
                Type: member.Type,
                Name: member.Name,
                DisplayName: member.GetDisplayName(requiredSymbols.DisplayAttribute),
                Attributes: attributes));
        }

        return [.. members];
    }

    public ImmutableArray<ITypeSymbol> ExtractPropertyTypes(ITypeSymbol type, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var processed = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        void Traverse(ITypeSymbol currentType)
        {
            if (currentType == null || currentType.SpecialType != SpecialType.None || processed.Contains(currentType))
            {
                return;
            }

            processed.Add(currentType);
            builder.Add(currentType);

            foreach (var member in currentType.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.Type is ITypeSymbol propertyType)
                {
                    Traverse(propertyType);
                }
            }
        }

        Traverse(type);
        return builder.ToImmutable();
    }

    internal static ImmutableArray<ValidationAttribute> ExtractValidationAttributes(ISymbol symbol, RequiredSymbols requiredSymbols, out bool isRequired)
    {
        var attributes = symbol.GetAttributes();
        if (attributes.Length == 0)
        {
            isRequired = false;
            return [];
        }

        // Continue with existing logic...
        var validationAttributes = attributes
            .Where(attribute => attribute.AttributeClass != null)
            .Where(attribute => attribute.AttributeClass!.ImplementsValidationAttribute(requiredSymbols.ValidationAttribute));
        isRequired = validationAttributes.Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, requiredSymbols.RequiredAttribute));
        return [.. validationAttributes
            .Where(attr => !SymbolEqualityComparer.Default.Equals(attr.AttributeClass, requiredSymbols.ValidationAttribute))
            .Select(attribute => new ValidationAttribute(
                Name: symbol.Name + attribute.AttributeClass!.Name,
                ClassName: attribute.AttributeClass!.ToDisplayString(_symbolDisplayFormat),
                Arguments: [.. attribute.ConstructorArguments.Select(a => a.ToCSharpString())],
                NamedArguments: attribute.NamedArguments.ToDictionary(namedArgument => namedArgument.Key, namedArgument => namedArgument.Value.ToCSharpString()),
                IsCustomValidationAttribute: SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, requiredSymbols.CustomValidationAttribute)))];
    }
}
