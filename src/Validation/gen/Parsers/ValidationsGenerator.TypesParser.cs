// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal static ImmutableArray<ValidatableType> ExtractValidatableTypes(IInvocationOperation operation)
    {
        AnalyzerDebug.Assert(operation.SemanticModel != null, "SemanticModel should not be null.");
        var parameters = operation.TryGetRouteHandlerMethod(operation.SemanticModel, out var method)
            ? method.Parameters
            : [];

        if (parameters.IsEmpty)
        {
            return [];
        }

        var wellKnownTypes = WellKnownTypes.GetOrCreate(operation.SemanticModel.Compilation);

        var fromServiceMetadataSymbol = wellKnownTypes.GetOptional(
            WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromKeyedServiceAttributeSymbol = wellKnownTypes.GetOptional(
            WellKnownTypeData.WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute);
        var skipValidationAttributeSymbol = wellKnownTypes.Get(
            WellKnownTypeData.WellKnownType.Microsoft_Extensions_Validation_SkipValidationAttribute);

        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];

        foreach (var parameter in parameters)
        {
            // Skip parameters that are injected as services
            if (parameter.IsServiceParameter(fromServiceMetadataSymbol, fromKeyedServiceAttributeSymbol))
            {
                continue;
            }

            // Skip method parameter if it or its type are annotated with SkipValidationAttribute
            if (parameter.IsSkippedValidationParameter(skipValidationAttributeSymbol))
            {
                continue;
            }

            _ = TryExtractValidatableType(parameter.Type, wellKnownTypes, validatableTypes, visitedTypes);
        }
        return [.. validatableTypes];
    }

    internal static bool TryExtractValidatableType(ITypeSymbol incomingTypeSymbol, WellKnownTypes wellKnownTypes, HashSet<ValidatableType> validatableTypes, List<ITypeSymbol> visitedTypes)
    {
        var typeSymbol = incomingTypeSymbol.UnwrapType(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Collections_IEnumerable));
        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return false;
        }

        if (visitedTypes.Contains(typeSymbol))
        {
            return true;
        }

        if (typeSymbol.IsExemptType(wellKnownTypes))
        {
            return false;
        }

        // Skip types that are not accessible from generated code
        if (typeSymbol.DeclaredAccessibility is not Accessibility.Public)
        {
            return false;
        }

        visitedTypes.Add(typeSymbol);

        var hasTypeLevelValidation = HasValidationAttributes(typeSymbol, wellKnownTypes) || HasIValidatableObjectInterface(typeSymbol, wellKnownTypes);

        // Extract validatable types discovered in base types of this type and add them to the top-level list.
        var current = typeSymbol.BaseType;
        var hasValidatableBaseType = false;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            hasValidatableBaseType |= TryExtractValidatableType(current, wellKnownTypes, validatableTypes, visitedTypes);
            current = current.BaseType;
        }

        // Extract validatable types discovered in members of this type and add them to the top-level list.
        ImmutableArray<ValidatableProperty> members = [];
        if (ParsabilityHelper.GetParsability(typeSymbol, wellKnownTypes) is Parsability.NotParsable)
        {
            members = ExtractValidatableMembers(typeSymbol, wellKnownTypes, validatableTypes, visitedTypes);
        }

        // Extract the validatable types discovered in the JsonDerivedTypeAttributes of this type and add them to the top-level list.
        var derivedTypes = typeSymbol.GetJsonDerivedTypes(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Text_Json_Serialization_JsonDerivedTypeAttribute));
        var hasValidatableDerivedTypes = false;
        foreach (var derivedType in derivedTypes ?? [])
        {
            hasValidatableDerivedTypes |= TryExtractValidatableType(derivedType, wellKnownTypes, validatableTypes, visitedTypes);
        }

        // No validatable members or derived types found, so we don't need to add this type.
        if (members.IsDefaultOrEmpty && !hasTypeLevelValidation && !hasValidatableBaseType && !hasValidatableDerivedTypes)
        {
            return false;
        }

        // Add the type itself as a validatable type itself.
        validatableTypes.Add(new ValidatableType(
            TypeFQN: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Members: members));

        return true;
    }

    private static ImmutableArray<ValidatableProperty> ExtractValidatableMembers(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, HashSet<ValidatableType> validatableTypes, List<ITypeSymbol> visitedTypes)
    {
        var members = new List<ValidatableProperty>();
        var resolvedRecordProperty = new List<IPropertySymbol>();

        var fromServiceMetadataSymbol = wellKnownTypes.GetOptional(
            WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromKeyedServiceAttributeSymbol = wellKnownTypes.GetOptional(
            WellKnownTypeData.WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute);
        var jsonIgnoreAttributeSymbol = wellKnownTypes.Get(
            WellKnownTypeData.WellKnownType.System_Text_Json_Serialization_JsonIgnoreAttribute);
        var skipValidationAttributeSymbol = wellKnownTypes.Get(
            WellKnownTypeData.WellKnownType.Microsoft_Extensions_Validation_SkipValidationAttribute);

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

                        // Skip parameters that are injected as services
                        if (parameter.IsServiceParameter(fromServiceMetadataSymbol, fromKeyedServiceAttributeSymbol))
                        {
                            continue;
                        }

                        // Skip primary constructor parameter if it or its type are annotated with SkipValidationAttribute
                        if (parameter.IsSkippedValidationParameter(skipValidationAttributeSymbol))
                        {
                            continue;
                        }

                        // Skip properties that are not accessible from generated code
                        if (correspondingProperty.DeclaredAccessibility is not Accessibility.Public)
                        {
                            continue;
                        }

                        // Skip properties that have JsonIgnore attribute
                        if (correspondingProperty.IsJsonIgnoredProperty(jsonIgnoreAttributeSymbol))
                        {
                            continue;
                        }

                        // Check if the property's type is validatable, this resolves
                        // validatable types in the inheritance hierarchy
                        _ = TryExtractValidatableType(
                            correspondingProperty.Type,
                            wellKnownTypes,
                            validatableTypes,
                            visitedTypes);

                        members.Add(new ValidatableProperty(
                            ContainingTypeFQN: correspondingProperty.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            TypeFQN: correspondingProperty.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            Name: correspondingProperty.Name,
                            DisplayName: parameter.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute)) ??
                                        correspondingProperty.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute))));
                    }
                }
            }
        }

        // Handle properties for classes and any properties not handled by the constructor
        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip compiler generated properties, indexers, static properties, properties without
            // a public getter, and properties already processed via the record processing logic above.
            if (member.IsImplicitlyDeclared
                || member.IsIndexer
                || member.IsStatic
                || member.IsWriteOnly
                || member.GetMethod is null
                || member.GetMethod.DeclaredAccessibility is not Accessibility.Public
                || member.IsEqualityContract(wellKnownTypes)
                || resolvedRecordProperty.Contains(member, SymbolEqualityComparer.Default))
            {
                continue;
            }

            // Skip properties that are injected as services
            if (member.IsServiceProperty(fromServiceMetadataSymbol, fromKeyedServiceAttributeSymbol))
            {
                continue;
            }

            // Skip properties that are not accessible from generated code
            if (member.DeclaredAccessibility is not Accessibility.Public)
            {
                continue;
            }

            // Skip properties that have JsonIgnore attribute
            if (member.IsJsonIgnoredProperty(jsonIgnoreAttributeSymbol))
            {
                continue;
            }

            // Skip property if it or its type are annotated with SkipValidationAttribute
            if (member.IsSkippedValidationProperty(skipValidationAttributeSymbol))
            {
                continue;
            }

            var hasValidatableType = TryExtractValidatableType(member.Type, wellKnownTypes, validatableTypes, visitedTypes);

            // If the member has no validation attributes or validatable types, skip it.
            if (!HasValidationAttributes(member, wellKnownTypes) && !hasValidatableType)
            {
                continue;
            }

            members.Add(new ValidatableProperty(
                ContainingTypeFQN: member.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TypeFQN: member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Name: member.Name,
                DisplayName: member.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute))));
        }

        return [.. members];
    }

    internal static bool HasValidationAttributes(ISymbol symbol, WellKnownTypes wellKnownTypes)
    {
        var validationAttributeSymbol = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_ValidationAttribute);

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is not null &&
                attribute.AttributeClass.ImplementsValidationAttribute(validationAttributeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasIValidatableObjectInterface(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes)
    {
        var validatableObjectSymbol = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_IValidatableObject);
        return typeSymbol.ImplementsInterface(validatableObjectSymbol);
    }
}
