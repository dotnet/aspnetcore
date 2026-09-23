// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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

internal enum InferredAlternativeSource
{
    None,
    Polymorphism,
    Union,
}

internal enum InferredAlternativeReason
{
    NoAlternatives,
    UnionCaseJsonDomainIsUnknown,
    UnionCasesHaveOverlappingJsonDomains,
    UnionCasesHaveDisjointJsonDomains,
    MissingDiscriminator,
    DuplicateDiscriminator,
    DistinctExplicitDiscriminators,
}

[Flags]
internal enum InferredJsonValueDomain
{
    None = 0,
    Null = 1 << 0,
    Boolean = 1 << 1,
    String = 1 << 2,
    Integer = 1 << 3,
    NonIntegerNumber = 1 << 4,
    Object = 1 << 5,
    Array = 1 << 6,
    Any = Null | Boolean | String | Integer | NonIntegerNumber | Object | Array,
}

internal enum InferredJsonDomainReason
{
    KnownPrimitive,
    SerializerObject,
    SerializerArray,
    UnionOfKnownDomains,
    CustomConverter,
    EnumRepresentation,
    FiniteSchemaValues,
    PolymorphicContract,
    ArbitraryJsonValue,
    UnsupportedScalar,
    UnsupportedNumberHandling,
    RecursiveUnion,
}

internal readonly record struct InferredJsonDomainFact(
    InferredJsonValueDomain Domains,
    bool IsExact,
    InferredJsonDomainReason Reason,
    InferredJsonFiniteDomainFact? FiniteDomain = null);

internal sealed record InferredAlternativeBranch(
    InferredSchemaTypeIdentity Identity,
    object? Discriminator,
    InferredJsonDomainFact? JsonDomain);

internal sealed record InferredAlternativeCompositionDecision(
    InferredAlternativeSource Source,
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
        var jsonDomains = new InferredJsonDomainResolver(document);
        foreach (var shape in document.Shapes)
        {
            decisions.Add(new(
                shape.Identity,
                DecideInheritance(document, shape),
                DecideAlternatives(shape, jsonDomains),
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

    private static InferredAlternativeCompositionDecision DecideAlternatives(
        InferredSchemaShape shape,
        InferredJsonDomainResolver jsonDomains)
    {
        if (shape.Kind == InferredSchemaShapeKind.Union)
        {
            var unionBranches = shape.UnionCases
                .Select(unionCase => new InferredAlternativeBranch(
                    unionCase.Identity,
                    null,
                    jsonDomains.GetDomain(unionCase)))
                .ToArray();

            if (unionBranches.Any(branch => branch.JsonDomain is not { IsExact: true }))
            {
                return UnionDecision(
                    InferredAlternativeCompositionKind.AnyOf,
                    InferredAlternativeReason.UnionCaseJsonDomainIsUnknown);
            }

            for (var i = 0; i < unionBranches.Length; i++)
            {
                for (var j = i + 1; j < unionBranches.Length; j++)
                {
                    if (DomainsOverlap(
                        unionBranches[i].JsonDomain!.Value,
                        unionBranches[j].JsonDomain!.Value))
                    {
                        return UnionDecision(
                            InferredAlternativeCompositionKind.AnyOf,
                            InferredAlternativeReason.UnionCasesHaveOverlappingJsonDomains);
                    }
                }
            }

            return UnionDecision(
                InferredAlternativeCompositionKind.OneOf,
                InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains);

            InferredAlternativeCompositionDecision UnionDecision(
                InferredAlternativeCompositionKind kind,
                InferredAlternativeReason reason)
                => new(
                    InferredAlternativeSource.Union,
                    kind,
                    reason,
                    null,
                    Array.AsReadOnly(unionBranches));
        }

        if (shape.DerivedTypes.Count == 0)
        {
            return new(
                InferredAlternativeSource.None,
                InferredAlternativeCompositionKind.None,
                InferredAlternativeReason.NoAlternatives,
                null,
                Array.Empty<InferredAlternativeBranch>());
        }

        var branches = shape.DerivedTypes
            .Select(derivedType => new InferredAlternativeBranch(derivedType.Identity, derivedType.Discriminator, null))
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
            InferredAlternativeSource.Polymorphism,
            InferredAlternativeCompositionKind.OneOf,
            InferredAlternativeReason.DistinctExplicitDiscriminators,
            discriminatorPropertyName,
            Array.AsReadOnly(branches));

        InferredAlternativeCompositionDecision AnyOf(InferredAlternativeReason reason)
            => new(
                InferredAlternativeSource.Polymorphism,
                InferredAlternativeCompositionKind.AnyOf,
                reason,
                discriminatorPropertyName,
                Array.AsReadOnly(branches));
    }

    private static bool DomainsOverlap(
        InferredJsonDomainFact left,
        InferredJsonDomainFact right)
    {
        if ((left.Domains & right.Domains) == InferredJsonValueDomain.None)
        {
            return false;
        }

        if (left.FiniteDomain is not { IsExact: true } leftFinite ||
            right.FiniteDomain is not { IsExact: true } rightFinite)
        {
            return true;
        }

        return leftFinite.Values.Intersect(rightFinite.Values).Any();
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

    private sealed class InferredJsonDomainResolver(InferredSchemaDocument document)
    {
        private readonly Dictionary<Type, InferredJsonDomainFact> _domains = [];
        private readonly HashSet<Type> _activeUnions = [];

        public InferredJsonDomainFact GetDomain(InferredSchemaTypeUse typeUse)
        {
            var domain = GetNonNullDomain(typeUse.Identity.Type);
            if (!typeUse.AllowsNull || !domain.IsExact)
            {
                return domain;
            }

            if (domain.FiniteDomain is { IsExact: true } finiteDomain)
            {
                var values = finiteDomain.Values
                    .Append(new(InferredJsonLiteralKind.Null, "null"))
                    .OrderBy(literal => literal.Kind)
                    .ThenBy(literal => literal.CanonicalValue, StringComparer.Ordinal)
                    .ToArray();
                domain = domain with
                {
                    FiniteDomain = finiteDomain with
                    {
                        Values = new ReadOnlyCollection<InferredJsonLiteral>(values),
                    },
                };
            }

            return domain with { Domains = domain.Domains | InferredJsonValueDomain.Null };
        }

        private InferredJsonDomainFact GetNonNullDomain(Type type)
        {
            if (_domains.TryGetValue(type, out var domain))
            {
                return domain;
            }

            domain = Classify(type);
            _domains.Add(type, domain);
            return domain;
        }

        private InferredJsonDomainFact Classify(Type type)
        {
            if (!document.TryGetShape(type, out var shape))
            {
                return Unknown(InferredJsonDomainReason.UnsupportedScalar);
            }

            if (shape.HasCustomConverter)
            {
                return Unknown(InferredJsonDomainReason.CustomConverter);
            }

            if (shape.DerivedTypes.Count > 0)
            {
                return Unknown(InferredJsonDomainReason.PolymorphicContract);
            }

            if (type.IsEnum)
            {
                return ClassifyEnum(shape);
            }

            if (type == typeof(object) ||
                type == typeof(JsonElement) ||
                type == typeof(JsonDocument) ||
                typeof(JsonNode).IsAssignableFrom(type))
            {
                return Unknown(InferredJsonDomainReason.ArbitraryJsonValue);
            }

            if (type == typeof(string) || type == typeof(char) || type == typeof(byte[]))
            {
                return Exact(InferredJsonValueDomain.String, InferredJsonDomainReason.KnownPrimitive);
            }

            if (type == typeof(bool))
            {
                return Exact(InferredJsonValueDomain.Boolean, InferredJsonDomainReason.KnownPrimitive);
            }

            if (type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(nint) ||
                type == typeof(nuint) ||
                type == typeof(Int128) ||
                type == typeof(UInt128))
            {
                return Numeric(
                    shape,
                    InferredJsonValueDomain.Integer,
                    supportsNamedFloatingPointLiterals: false);
            }

            if (type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(Half))
            {
                return Numeric(
                    shape,
                    InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber,
                    supportsNamedFloatingPointLiterals: true);
            }

            if (type == typeof(decimal))
            {
                return Numeric(
                    shape,
                    InferredJsonValueDomain.Integer | InferredJsonValueDomain.NonIntegerNumber,
                    supportsNamedFloatingPointLiterals: false);
            }

            return shape.Kind switch
            {
                InferredSchemaShapeKind.Object or InferredSchemaShapeKind.Dictionary =>
                    Exact(InferredJsonValueDomain.Object, InferredJsonDomainReason.SerializerObject),
                InferredSchemaShapeKind.Collection =>
                    Exact(InferredJsonValueDomain.Array, InferredJsonDomainReason.SerializerArray),
                InferredSchemaShapeKind.Tuple =>
                    Exact(InferredJsonValueDomain.Array, InferredJsonDomainReason.SerializerArray),
                InferredSchemaShapeKind.Union => ClassifyUnion(shape),
                _ => Unknown(InferredJsonDomainReason.UnsupportedScalar),
            };
        }

        private InferredJsonDomainFact ClassifyUnion(InferredSchemaShape shape)
        {
            if (!_activeUnions.Add(shape.Identity.Type))
            {
                return Unknown(InferredJsonDomainReason.RecursiveUnion);
            }

            try
            {
                var domains = InferredJsonValueDomain.None;
                List<InferredJsonLiteral>? finiteValues = [];
                foreach (var unionCase in shape.UnionCases)
                {
                    var domain = GetDomain(unionCase);
                    if (!domain.IsExact)
                    {
                        return domain;
                    }

                    domains |= domain.Domains;
                    if (domain.FiniteDomain is { IsExact: true } caseFiniteDomain)
                    {
                        finiteValues?.AddRange(caseFiniteDomain.Values);
                    }
                    else
                    {
                        finiteValues = null;
                    }
                }

                var finiteDomain = finiteValues is null
                    ? null
                    : new InferredJsonFiniteDomainFact(
                        IsExact: true,
                        InferredJsonFiniteDomainReason.UnionOfFiniteDomains,
                        new ReadOnlyCollection<InferredJsonLiteral>(
                            finiteValues
                                .Distinct()
                                .OrderBy(literal => literal.Kind)
                                .ThenBy(literal => literal.CanonicalValue, StringComparer.Ordinal)
                                .ToArray()));
                return new(domains, IsExact: true, InferredJsonDomainReason.UnionOfKnownDomains, finiteDomain);
            }
            finally
            {
                _activeUnions.Remove(shape.Identity.Type);
            }
        }

        private static InferredJsonDomainFact ClassifyEnum(InferredSchemaShape shape)
        {
            if (shape.FiniteDomain is not { IsExact: true } finiteDomain)
            {
                return Unknown(InferredJsonDomainReason.EnumRepresentation);
            }

            var domains = InferredJsonValueDomain.None;
            foreach (var value in finiteDomain.Values)
            {
                domains |= value.Kind switch
                {
                    InferredJsonLiteralKind.Null => InferredJsonValueDomain.Null,
                    InferredJsonLiteralKind.Boolean => InferredJsonValueDomain.Boolean,
                    InferredJsonLiteralKind.Number when value.IsInteger => InferredJsonValueDomain.Integer,
                    InferredJsonLiteralKind.Number => InferredJsonValueDomain.NonIntegerNumber,
                    InferredJsonLiteralKind.String => InferredJsonValueDomain.String,
                    _ => InferredJsonValueDomain.Any,
                };
            }

            return new(domains, IsExact: true, InferredJsonDomainReason.FiniteSchemaValues, finiteDomain);
        }

        private static InferredJsonDomainFact Exact(
            InferredJsonValueDomain domains,
            InferredJsonDomainReason reason)
            => new(domains, IsExact: true, reason);

        private static InferredJsonDomainFact Numeric(
            InferredSchemaShape shape,
            InferredJsonValueDomain numericDomains,
            bool supportsNamedFloatingPointLiterals)
        {
            const JsonNumberHandling supportedNumberHandling =
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.WriteAsString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;
            if ((shape.NumberHandling & ~supportedNumberHandling) != 0)
            {
                return Unknown(InferredJsonDomainReason.UnsupportedNumberHandling);
            }

            var permitsString =
                (shape.NumberHandling &
                    (JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)) != 0 ||
                supportsNamedFloatingPointLiterals &&
                (shape.NumberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0;
            return Exact(
                permitsString ? numericDomains | InferredJsonValueDomain.String : numericDomains,
                InferredJsonDomainReason.KnownPrimitive);
        }

        private static InferredJsonDomainFact Unknown(InferredJsonDomainReason reason)
            => new(InferredJsonValueDomain.Any, IsExact: false, reason);
    }
}
