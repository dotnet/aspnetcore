// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        if (typeSymbol.IsExemptType(requiredSymbols))
        {
            return false;
        }

        visitedTypes.Add(typeSymbol);

        // Extract validatable types discovered in base types of this type and add them to the top-level list.
        var current = typeSymbol.BaseType;
        var hasValidatableBaseType = false;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            hasValidatableBaseType |= TryExtractValidatableType(current, requiredSymbols, ref validatableTypes, ref visitedTypes);
            current = current.BaseType;
        }

        // Extract validatable types discovered in members of this type and add them to the top-level list.
        var members = ExtractValidatableMembers(typeSymbol, requiredSymbols, ref validatableTypes, ref visitedTypes);

        // Extract the validatable types discovered in the JsonDerivedTypeAttributes of this type and add them to the top-level list.
        var derivedTypes = typeSymbol.GetJsonDerivedTypes(requiredSymbols.JsonDerivedTypeAttribute);
        var hasValidatableDerivedTypes = false;
        foreach (var derivedType in derivedTypes ?? [])
        {
            hasValidatableDerivedTypes |= TryExtractValidatableType(derivedType, requiredSymbols, ref validatableTypes, ref visitedTypes);
        }

        // No validatable members or derived types found, so we don't need to add this type.
        if (members.IsDefaultOrEmpty && !hasValidatableBaseType && !hasValidatableDerivedTypes)
        {
            return false;
        }

        // Add the type itself as a validatable type itself.
        validatableTypes.Add(new ValidatableType(
            Type: typeSymbol,
            Members: members));

        return true;
    }

    internal ImmutableArray<ValidatableProperty> ExtractValidatableMembers(ITypeSymbol typeSymbol, RequiredSymbols requiredSymbols, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
    {
        var members = new List<ValidatableProperty>();
        var resolvedRecordProperty = new List<IPropertySymbol>();

        // Special handling for record types to extract properties from
        // the primary constructor.
        if (typeSymbol is INamedTypeSymbol { IsRecord: true } namedType)
        {
            // Find the primary constructor for the record, account
            // for members that are in base types to account for
            // record inheritance scenarios
            var primaryConstructor = namedType.Constructors
                .FirstOrDefault(c => c.Parameters.Length > 0 && c.Parameters.All(p =>
                    namedType.FindPropertyIncludingBaseTypes(p.Name) != null));

            if (primaryConstructor != null)
            {
                // Process all parameters in constructor order to maintain parameter ordering
                foreach (var parameter in primaryConstructor.Parameters)
                {
                    // Find the corresponding property in this type, we ignore
                    // base types here since that will be handled by the inheritance
                    // checks in the default ValidatableTypeInfo implementation.
                    var correspondingProperty = typeSymbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault(p => string.Equals(p.Name, parameter.Name, System.StringComparison.OrdinalIgnoreCase));

                    if (correspondingProperty != null)
                    {
                        resolvedRecordProperty.Add(correspondingProperty);

                        // Check if the property's type is validatable, this resolves
                        // validatable types in the inheritance hierarchy
                        var hasValidatableType = TryExtractValidatableType(
                            correspondingProperty.Type.UnwrapType(requiredSymbols.IEnumerable),
                            requiredSymbols,
                            ref validatableTypes,
                            ref visitedTypes);

                        members.Add(new ValidatableProperty(
                            ContainingType: correspondingProperty.ContainingType,
                            Type: correspondingProperty.Type,
                            Name: correspondingProperty.Name,
                            DisplayName: parameter.GetDisplayName(requiredSymbols.DisplayAttribute) ??
                                        correspondingProperty.GetDisplayName(requiredSymbols.DisplayAttribute),
                            Attributes: []));
                    }
                }
            }
        }

        // Handle properties for classes and any properties not handled by the constructor
        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip compiler generated properties and properties already processed via
            // the record processing logic above.
            if (member.IsImplicitlyDeclared || resolvedRecordProperty.Contains(member, SymbolEqualityComparer.Default))
            {
                continue;
            }

            var hasValidatableType = TryExtractValidatableType(member.Type.UnwrapType(requiredSymbols.IEnumerable), requiredSymbols, ref validatableTypes, ref visitedTypes);
            var attributes = ExtractValidationAttributes(member, requiredSymbols, out var isRequired);

            // If the member has no validation attributes or validatable types and is not required, skip it.
            if (attributes.IsDefaultOrEmpty && !hasValidatableType && !isRequired)
            {
                continue;
            }

            members.Add(new ValidatableProperty(
                ContainingType: member.ContainingType,
                Type: member.Type,
                Name: member.Name,
                DisplayName: member.GetDisplayName(requiredSymbols.DisplayAttribute),
                Attributes: attributes));
        }

        return [.. members];
    }

    internal static ImmutableArray<ValidationAttribute> ExtractValidationAttributes(ISymbol symbol, RequiredSymbols requiredSymbols, out bool isRequired)
    {
        var attributes = symbol.GetAttributes();
        if (attributes.Length == 0)
        {
            isRequired = false;
            return [];
        }

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
