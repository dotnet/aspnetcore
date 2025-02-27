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

    internal ImmutableArray<ValidatableParameter> ExtractParameters(IInvocationOperation operation, RequiredSymbols requiredSymbols, ref HashSet<ValidatableType> validatableTypes)
    {
        AnalyzerDebug.Assert(operation.SemanticModel != null, "SemanticModel should not be null.");
        var parameters = operation.TryGetRouteHandlerMethod(operation.SemanticModel, out var method)
            ? method.Parameters
            : [];
        var validatableParameters = ImmutableArray.CreateBuilder<ValidatableParameter>(parameters.Length);
        List<ITypeSymbol> visitedTypes = [];
        foreach (var parameter in parameters)
        {
            var hasValidatableType = TryExtractValidatableType(parameter.Type.UnwrapType(requiredSymbols.IEnumerable), requiredSymbols, ref validatableTypes, ref visitedTypes);
            validatableParameters.Add(new ValidatableParameter(
                Type: parameter.Type.UnwrapType(requiredSymbols.IEnumerable),
                OriginalType: parameter.Type,
                Name: parameter.Name,
                DisplayName: parameter.GetDisplayName(requiredSymbols.DisplayAttribute),
                Index: parameter.Ordinal,
                IsNullable: parameter.Type.IsNullable(),
                IsEnumerable: parameter.Type.IsEnumerable(requiredSymbols.IEnumerable),
                Attributes: ExtractValidationAttributes(parameter, requiredSymbols),
                HasValidatableType: hasValidatableType));
        }
        return validatableParameters.ToImmutable();
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
        List<string>? validatableSubTypes = [];
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            _ = TryExtractValidatableType(current, requiredSymbols, ref validatableTypes, ref visitedTypes);
            validatableSubTypes.Add(current.Name);
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
            ValidatableSubTypeNames: [.. validatableSubTypes],
            ValidatableDerivedTypeNames: [.. derivedTypeNames]));

        return true;
    }

    internal ImmutableArray<ValidatableMember> ExtractValidatableMembers(ITypeSymbol typeSymbol, RequiredSymbols requiredSymbols, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
    {
        var members = new List<ValidatableMember>();
        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var hasValidatableType = TryExtractValidatableType(member.Type.UnwrapType(requiredSymbols.IEnumerable), requiredSymbols, ref validatableTypes, ref visitedTypes);
            members.Add(new ValidatableMember(
                ParentType: member.ContainingType,
                Name: member.Name,
                DisplayName: member.GetDisplayName(requiredSymbols.DisplayAttribute),
                IsEnumerable: member.Type.IsEnumerable(requiredSymbols.IEnumerable),
                IsNullable: member.Type.IsNullable(),
                Attributes: ExtractValidationAttributes(member, requiredSymbols),
                HasValidatableType: hasValidatableType));
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

    internal static ImmutableArray<ValidationAttribute> ExtractValidationAttributes(IPropertySymbol property, RequiredSymbols requiredSymbols)
    {
        return [.. property.GetAttributes()
            .Where(attribute => attribute.AttributeClass != null)
            .Where(attribute => attribute.AttributeClass!.ImplementsValidationAttribute(requiredSymbols.ValidationAttribute))
            .Select(attribute => new ValidationAttribute(
                Name: property.Type.Name + property.Name + attribute.AttributeClass!.Name,
                ClassName: attribute.AttributeClass!.ToDisplayString(_symbolDisplayFormat),
                Arguments: [.. attribute.ConstructorArguments.Select(a => a.ToCSharpString())],
                NamedArguments: attribute.NamedArguments.ToDictionary(namedArgument => namedArgument.Key, namedArgument => namedArgument.Value.ToCSharpString())))];
    }

    internal static ImmutableArray<ValidationAttribute> ExtractValidationAttributes(IParameterSymbol parameter, RequiredSymbols requiredSymbols)
    {
        return [.. parameter.GetAttributes()
            .Where(attribute => attribute.AttributeClass != null)
            .Where(attribute => attribute.AttributeClass!.ImplementsValidationAttribute(requiredSymbols.ValidationAttribute))
            .Select(attribute => new ValidationAttribute(
                Name: parameter.Name + attribute.AttributeClass!.Name,
                ClassName: attribute.AttributeClass!.ToDisplayString(_symbolDisplayFormat),
                Arguments: [.. attribute.ConstructorArguments.Select(a => a.ToCSharpString())],
                NamedArguments: attribute.NamedArguments.ToDictionary(namedArgument => namedArgument.Key, namedArgument => namedArgument.Value.ToCSharpString()),
                ForParameter: true))];
    }
}
