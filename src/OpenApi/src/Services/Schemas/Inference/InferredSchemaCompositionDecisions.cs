// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.AspNetCore.OpenApi;

internal enum InferredInheritanceReason
{
    Eligible,
    NoBaseType,
    BaseShapeUnavailable,
    DerivedContractIsNotObject,
    BaseContractIsNotObject,
    CustomConverter,
    PropertyNameCollision,
    PropertyHiding,
    ExtensionData,
    AdditionalProperties,
    PolymorphicHierarchy,
    BaseContractMismatch,
}

internal sealed record InferredInheritanceCompositionDecision(
    bool IsEligible,
    InferredSchemaTypeIdentity? BaseType,
    InferredInheritanceReason Reason);

internal enum InferredAlternativeCompositionKind
{
    None,
    AnyOf,
    OneOf,
}

internal enum InferredAlternativeReason
{
    NoAlternatives,
    UnionCasesAreNotProvenExclusive,
    MissingDiscriminator,
    DuplicateDiscriminator,
    DistinctExplicitDiscriminators,
}

internal sealed record InferredAlternativeBranch(
    InferredSchemaTypeIdentity Identity,
    object? Discriminator);

internal sealed record InferredAlternativeCompositionDecision(
    InferredAlternativeCompositionKind Kind,
    InferredAlternativeReason Reason,
    string? DiscriminatorPropertyName,
    IReadOnlyList<InferredAlternativeBranch> Branches);

internal enum InferredObjectContractKind
{
    NotObject,
    Closed,
    DisallowUnmappedMembers,
    ExtensionData,
}

internal sealed record InferredObjectContractDecision(
    InferredObjectContractKind Kind,
    InferredSchemaProperty? ExtensionDataProperty,
    InferredSchemaTypeUse? AdditionalPropertiesType);

internal sealed record InferredSchemaCompositionDecision(
    InferredSchemaTypeIdentity Identity,
    InferredInheritanceCompositionDecision Inheritance,
    InferredAlternativeCompositionDecision Alternatives,
    InferredObjectContractDecision ObjectContract);

internal sealed class InferredSchemaCompositionDecisions
{
    private readonly IReadOnlyDictionary<Type, InferredSchemaCompositionDecision> _decisions;

    public InferredSchemaCompositionDecisions(IReadOnlyList<InferredSchemaCompositionDecision> decisions)
    {
        Decisions = decisions;
        _decisions = new ReadOnlyDictionary<Type, InferredSchemaCompositionDecision>(
            decisions.ToDictionary(decision => decision.Identity.Type));
    }

    public IReadOnlyList<InferredSchemaCompositionDecision> Decisions { get; }

    public InferredSchemaCompositionDecision this[Type type] => _decisions[Nullable.GetUnderlyingType(type) ?? type];
}

internal static class InferredSchemaCompositionDecisionBuilder
{
    public static InferredSchemaCompositionDecisions Build(InferredSchemaDocument document)
    {
        var decisions = new List<InferredSchemaCompositionDecision>(document.Shapes.Count);
        foreach (var shape in document.Shapes)
        {
            decisions.Add(new(
                shape.Identity,
                DecideInheritance(document, shape),
                DecideAlternatives(shape),
                DecideObjectContract(shape)));
        }

        return new(new ReadOnlyCollection<InferredSchemaCompositionDecision>(decisions));
    }

    private static InferredInheritanceCompositionDecision DecideInheritance(
        InferredSchemaDocument document,
        InferredSchemaShape derivedShape)
    {
        if (derivedShape.BaseType is not { } baseType)
        {
            return new(false, null, InferredInheritanceReason.NoBaseType);
        }

        if (!document.TryGetShape(baseType.Type, out var baseShape))
        {
            return new(false, baseType, InferredInheritanceReason.BaseShapeUnavailable);
        }

        if (derivedShape.HasCustomConverter || baseShape.HasCustomConverter)
        {
            return new(false, baseType, InferredInheritanceReason.CustomConverter);
        }

        if (derivedShape.Kind != InferredSchemaShapeKind.Object)
        {
            return new(false, baseType, InferredInheritanceReason.DerivedContractIsNotObject);
        }

        if (baseShape.Kind != InferredSchemaShapeKind.Object)
        {
            return new(false, baseType, InferredInheritanceReason.BaseContractIsNotObject);
        }

        if (derivedShape.AdditionalPropertiesType is not null || baseShape.AdditionalPropertiesType is not null)
        {
            return new(false, baseType, InferredInheritanceReason.ExtensionData);
        }

        if (derivedShape.DisallowsUnmappedMembers || baseShape.DisallowsUnmappedMembers)
        {
            return new(false, baseType, InferredInheritanceReason.AdditionalProperties);
        }

        if (derivedShape.DerivedTypes.Count > 0 || HasPolymorphicAncestor(document, baseShape))
        {
            return new(false, baseType, InferredInheritanceReason.PolymorphicHierarchy);
        }

        var basePropertiesByJsonName = baseShape.Properties.ToDictionary(
            property => property.Identity.JsonName,
            StringComparer.Ordinal);
        var baseMemberNames = baseShape.Properties
            .Select(property => property.Identity.MemberName)
            .ToHashSet(StringComparer.Ordinal);
        var localProperties = derivedShape.Properties
            .Where(property => property.Identity.DeclaringType.Type == derivedShape.Identity.Type);

        foreach (var property in localProperties)
        {
            if (basePropertiesByJsonName.ContainsKey(property.Identity.JsonName))
            {
                return new(false, baseType, InferredInheritanceReason.PropertyNameCollision);
            }

            if (baseMemberNames.Contains(property.Identity.MemberName))
            {
                return new(false, baseType, InferredInheritanceReason.PropertyHiding);
            }
        }

        foreach (var baseProperty in baseShape.Properties)
        {
            var derivedProperty = derivedShape.Properties.FirstOrDefault(
                property => StringComparer.Ordinal.Equals(property.Identity.JsonName, baseProperty.Identity.JsonName));
            if (derivedProperty is null ||
                derivedProperty.DeclaredPropertyType != baseProperty.DeclaredPropertyType ||
                derivedProperty.PropertyType != baseProperty.PropertyType ||
                derivedProperty.IsRequired != baseProperty.IsRequired ||
                derivedProperty.IsExtensionData != baseProperty.IsExtensionData)
            {
                return new(false, baseType, InferredInheritanceReason.BaseContractMismatch);
            }
        }

        return new(true, baseType, InferredInheritanceReason.Eligible);
    }

    private static bool HasPolymorphicAncestor(
        InferredSchemaDocument document,
        InferredSchemaShape shape)
    {
        while (true)
        {
            if (shape.DerivedTypes.Count > 0)
            {
                return true;
            }

            if (shape.BaseType is not { } baseType ||
                !document.TryGetShape(baseType.Type, out shape))
            {
                return false;
            }
        }
    }

    private static InferredAlternativeCompositionDecision DecideAlternatives(InferredSchemaShape shape)
    {
        if (shape.Kind == InferredSchemaShapeKind.Union)
        {
            var unionBranches = shape.UnionCases
                .Select(unionCase => new InferredAlternativeBranch(unionCase.Identity, null))
                .ToArray();
            return new(
                InferredAlternativeCompositionKind.AnyOf,
                InferredAlternativeReason.UnionCasesAreNotProvenExclusive,
                null,
                Array.AsReadOnly(unionBranches));
        }

        if (shape.DerivedTypes.Count == 0)
        {
            return new(
                InferredAlternativeCompositionKind.None,
                InferredAlternativeReason.NoAlternatives,
                null,
                Array.Empty<InferredAlternativeBranch>());
        }

        var branches = shape.DerivedTypes
            .Select(derivedType => new InferredAlternativeBranch(derivedType.Identity, derivedType.Discriminator))
            .ToArray();
        var discriminatorPropertyName = shape.DiscriminatorPropertyName;
        if (branches.Any(branch => branch.Discriminator is null))
        {
            return AnyOf(InferredAlternativeReason.MissingDiscriminator);
        }

        if (branches.Select(branch => branch.Discriminator).Distinct().Count() != branches.Length)
        {
            return AnyOf(InferredAlternativeReason.DuplicateDiscriminator);
        }

        return new(
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.DistinctExplicitDiscriminators,
            discriminatorPropertyName,
            Array.AsReadOnly(branches));

        InferredAlternativeCompositionDecision AnyOf(InferredAlternativeReason reason)
            => new(
                InferredAlternativeCompositionKind.AnyOf,
                reason,
                discriminatorPropertyName,
                Array.AsReadOnly(branches));
    }

    private static InferredObjectContractDecision DecideObjectContract(InferredSchemaShape shape)
    {
        if (shape.Kind != InferredSchemaShapeKind.Object)
        {
            return new(InferredObjectContractKind.NotObject, null, null);
        }

        if (shape.ExtensionDataProperty is { } extensionDataProperty)
        {
            if (shape.DisallowsUnmappedMembers)
            {
                throw new InvalidOperationException(
                    $"The serializer contract for '{shape.Identity.Type}' both disallows unmapped members and declares extension data.");
            }

            if (shape.AdditionalPropertiesType is not { } additionalPropertiesType)
            {
                throw new InvalidOperationException(
                    $"The extension-data value contract for '{shape.Identity.Type}' is unavailable.");
            }

            return new(
                InferredObjectContractKind.ExtensionData,
                extensionDataProperty,
                additionalPropertiesType);
        }

        return shape.DisallowsUnmappedMembers
            ? new(InferredObjectContractKind.DisallowUnmappedMembers, null, null)
            : new(InferredObjectContractKind.Closed, null, null);
    }
}
