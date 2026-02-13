// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat _symbolDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    internal ImmutableArray<ValidatableType> ExtractValidatableTypes(IInvocationOperation operation, WellKnownTypes wellKnownTypes)
    {
        AnalyzerDebug.Assert(operation.SemanticModel != null, "SemanticModel should not be null.");
        var parameters = operation.TryGetRouteHandlerMethod(operation.SemanticModel, out var method)
            ? method.Parameters
            : [];

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

            _ = TryExtractValidatableType(parameter.Type, wellKnownTypes, ref validatableTypes, ref visitedTypes);
        }
        return [.. validatableTypes];
    }

    internal bool TryExtractValidatableType(ITypeSymbol incomingTypeSymbol, WellKnownTypes wellKnownTypes, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
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

        // Type parameters (e.g., TRequest from a generic MapCommand<TRequest>() extension)
        // have DeclaredAccessibility == NotApplicable. The concrete type is only known at
        // call sites, not inside the generic method body where the endpoint delegate is
        // defined. Walk constraint types to discover any validatable types reachable
        // through type constraints.
        if (typeSymbol is ITypeParameterSymbol typeParam)
        {
            // Add to visitedTypes BEFORE iterating constraints to prevent
            // infinite recursion through circular constraints such as
            // where T : class, IEnumerable<T> (SEC-001).
            visitedTypes.Add(typeSymbol);
            var foundValidatable = false;
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                foundValidatable |= TryExtractValidatableType(constraintType, wellKnownTypes, ref validatableTypes, ref visitedTypes);
            }
            return foundValidatable;
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
            hasValidatableBaseType |= TryExtractValidatableType(current, wellKnownTypes, ref validatableTypes, ref visitedTypes);
            current = current.BaseType;
        }

        // Extract validatable types discovered in members of this type and add them to the top-level list.
        ImmutableArray<ValidatableProperty> members = [];
        if (ParsabilityHelper.GetParsability(typeSymbol, wellKnownTypes) is Parsability.NotParsable)
        {
            members = ExtractValidatableMembers(typeSymbol, wellKnownTypes, ref validatableTypes, ref visitedTypes);
        }

        // Extract the validatable types discovered in the JsonDerivedTypeAttributes of this type and add them to the top-level list.
        var derivedTypes = typeSymbol.GetJsonDerivedTypes(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Text_Json_Serialization_JsonDerivedTypeAttribute));
        var hasValidatableDerivedTypes = false;
        foreach (var derivedType in derivedTypes ?? [])
        {
            hasValidatableDerivedTypes |= TryExtractValidatableType(derivedType, wellKnownTypes, ref validatableTypes, ref visitedTypes);
        }

        // No validatable members or derived types found, so we don't need to add this type.
        if (members.IsDefaultOrEmpty && !hasTypeLevelValidation && !hasValidatableBaseType && !hasValidatableDerivedTypes)
        {
            return false;
        }

        // Add the type itself as a validatable type itself.
        validatableTypes.Add(new ValidatableType(
            Type: typeSymbol,
            Members: members));

        return true;
    }

    internal ImmutableArray<ValidatableProperty> ExtractValidatableMembers(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, ref HashSet<ValidatableType> validatableTypes, ref List<ITypeSymbol> visitedTypes)
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
                        var hasValidatableType = TryExtractValidatableType(
                            correspondingProperty.Type,
                            wellKnownTypes,
                            ref validatableTypes,
                            ref visitedTypes);

                        // Skip properties whose type is a type parameter (e.g., TSelf
                        // from CRTP pattern RequestBase<TSelf>). The emitter would
                        // generate typeof(TSelf) which is not valid C# (CRASH-001).
                        // Concrete validatable types reachable through constraints are
                        // already discovered by TryExtractValidatableType above.
                        if (ContainsTypeParameter(correspondingProperty.Type))
                        {
                            continue;
                        }

                        members.Add(new ValidatableProperty(
                            ContainingType: correspondingProperty.ContainingType,
                            Type: correspondingProperty.Type,
                            Name: correspondingProperty.Name,
                            DisplayName: parameter.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute)) ??
                                        correspondingProperty.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute)),
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
            if (member.IsImplicitlyDeclared
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

            var hasValidatableType = TryExtractValidatableType(member.Type, wellKnownTypes, ref validatableTypes, ref visitedTypes);
            var attributes = ExtractValidationAttributes(member, wellKnownTypes, out var isRequired);

            // If the member has no validation attributes or validatable types and is not required, skip it.
            if (attributes.IsDefaultOrEmpty && !hasValidatableType && !isRequired)
            {
                continue;
            }

            // Skip properties whose type is a type parameter (e.g., TSelf
            // from CRTP pattern RequestBase<TSelf>). The emitter would
            // generate typeof(TSelf) which is not valid C# (CRASH-001).
            if (ContainsTypeParameter(member.Type))
            {
                continue;
            }

            members.Add(new ValidatableProperty(
                ContainingType: member.ContainingType,
                Type: member.Type,
                Name: member.Name,
                DisplayName: member.GetDisplayName(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_DisplayAttribute)),
                Attributes: attributes));
        }

        return [.. members];
    }

    internal static ImmutableArray<ValidationAttribute> ExtractValidationAttributes(ISymbol symbol, WellKnownTypes wellKnownTypes, out bool isRequired)
    {
        var attributes = symbol.GetAttributes();
        if (attributes.Length == 0)
        {
            isRequired = false;
            return [];
        }

        var validationAttributes = attributes
            .Where(attribute => attribute.AttributeClass != null)
            .Where(attribute => attribute.AttributeClass!.ImplementsValidationAttribute(wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_ValidationAttribute)));
        isRequired = validationAttributes.Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_RequiredAttribute)));
        return [.. validationAttributes
            .Where(attr => !SymbolEqualityComparer.Default.Equals(attr.AttributeClass, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_ValidationAttribute)))
            .Select(attribute => new ValidationAttribute(
                Name: symbol.Name + attribute.AttributeClass!.Name,
                ClassName: attribute.AttributeClass!.ToDisplayString(_symbolDisplayFormat),
                Arguments: [.. attribute.ConstructorArguments.Select(a => a.ToCSharpString())],
                NamedArguments: attribute.NamedArguments.ToDictionary(namedArgument => namedArgument.Key, namedArgument => namedArgument.Value.ToCSharpString()),
                IsCustomValidationAttribute: SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_ComponentModel_DataAnnotations_CustomValidationAttribute))))];
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

    /// <summary>
    /// Returns true if the given type symbol contains an unresolved type parameter
    /// anywhere in its type tree. This catches not only bare <c>T</c> but also
    /// constructed types like <c>List&lt;T&gt;</c>, <c>T[]</c>, <c>T?</c>, and
    /// <c>Dictionary&lt;string, T&gt;</c> — all of which would produce invalid
    /// <c>typeof(...)</c> expressions in the emitted code.
    /// </summary>
    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        // Bare type parameter: T, TSelf, TSelf?
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        // Array: T[], T[,], List<T>[]
        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        // Constructed generic: List<T>, Dictionary<string, T>, Nullable<T>, Func<T, bool>
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (ContainsTypeParameter(typeArg))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
