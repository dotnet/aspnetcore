// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

#nullable enable

public partial class OpenApiSchemaServiceTests
{
    [Fact]
    public void CompositionDecision_SimpleInheritance_IsEligible()
    {
        var document = BuildCompositionShape<SimpleDerived>();

        var decision = document.CompositionDecisions[typeof(SimpleDerived)].Inheritance;
        Assert.True(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.Eligible, decision.Reason);
        Assert.Equal(typeof(SimpleBase), decision.BaseType?.Type);
    }

    [Fact]
    public void CompositionDecision_HiddenProperty_IsRejected()
    {
        var document = BuildCompositionShape<HiddenDerived>();

        var decision = document.CompositionDecisions[typeof(HiddenDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.PropertyHiding, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_CollidingProperty_IsRejected()
    {
        var document = BuildCompositionShape<CollidingDerived>();

        var decision = document.CompositionDecisions[typeof(CollidingDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.PropertyNameCollision, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_ExtensionData_IsRejected()
    {
        var document = BuildCompositionShape<ExtensionDataDerived>();

        var decision = document.CompositionDecisions[typeof(ExtensionDataDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.ExtensionData, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_AdditionalPropertiesConstraint_IsRejected()
    {
        var document = BuildCompositionShape<DisallowingDerived>();

        var decision = document.CompositionDecisions[typeof(DisallowingDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.AdditionalProperties, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_CustomConverter_IsRejected()
    {
        var document = BuildCompositionShape<ConvertedDerived>();

        var decision = document.CompositionDecisions[typeof(ConvertedDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.CustomConverter, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_PolymorphicBase_IsRejected()
    {
        var document = BuildCompositionShape<OrderedZetaDerived>();

        var decision = document.CompositionDecisions[typeof(OrderedZetaDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.PolymorphicHierarchy, decision.Reason);
    }

    [Theory]
    [InlineData(typeof(PolymorphicDerivedRoot))]
    [InlineData(typeof(DeepPolymorphicDerived))]
    public void CompositionDecision_PolymorphicHierarchy_IsRejected(Type type)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        var document = InferredSchemaShapeBuilder.Build(options, type);

        var decision = document.CompositionDecisions[type].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.PolymorphicHierarchy, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_BaseContractMismatch_IsRejected()
    {
        var resolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(MismatchedDerived))
            {
                typeInfo.Properties.Single(property => property.Name == "value").IsRequired = true;
            }
        });
        var document = BuildCompositionShape<MismatchedDerived>(resolver);

        var decision = document.CompositionDecisions[typeof(MismatchedDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.BaseContractMismatch, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_BaseShapeUnavailable_IsRejected()
    {
        var derivedShape = new InferredSchemaShape(
            new(typeof(UnavailableBaseDerived)),
            InferredSchemaShapeKind.Object,
            typeof(object),
            hasCustomConverter: false,
            numberHandling: JsonNumberHandling.Strict,
            disallowsUnmappedMembers: false,
            discriminatorPropertyName: null,
            baseType: new(typeof(UnavailableBase)),
            elementType: null,
            additionalPropertiesType: null,
            properties: Array.Empty<InferredSchemaProperty>(),
            derivedTypes: Array.Empty<InferredSchemaDerivedType>(),
            unionCases: Array.Empty<InferredSchemaTypeUse>());
        var document = new InferredSchemaDocument(
            new(new(typeof(UnavailableBaseDerived)), AllowsNull: false),
            [derivedShape]);

        var decision = document.CompositionDecisions[typeof(UnavailableBaseDerived)].Inheritance;
        Assert.False(decision.IsEligible);
        Assert.Equal(InferredInheritanceReason.BaseShapeUnavailable, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_FullyDiscriminatedPolymorphism_UsesOneOfInConfiguredOrder()
    {
        var document = BuildCompositionShape<OrderedPolymorphicBase>();

        var decision = document.CompositionDecisions[typeof(OrderedPolymorphicBase)].Alternatives;
        Assert.Equal(InferredAlternativeCompositionKind.OneOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.DistinctExplicitDiscriminators, decision.Reason);
        Assert.Equal("kind", decision.DiscriminatorPropertyName);
        Assert.Equal(
            [typeof(OrderedZetaDerived), typeof(OrderedAlphaDerived)],
            decision.Branches.Select(branch => branch.Identity.Type));
        Assert.Equal(["zeta", "alpha"], decision.Branches.Select(branch => branch.Discriminator));
    }

    [Fact]
    public void CompositionDecision_MissingDiscriminator_UsesAnyOf()
    {
        var document = BuildCompositionShape<PartiallyDiscriminatedBase>();

        var decision = document.CompositionDecisions[typeof(PartiallyDiscriminatedBase)].Alternatives;
        Assert.Equal(InferredAlternativeCompositionKind.AnyOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.MissingDiscriminator, decision.Reason);
    }

    [Fact]
    public void CompositionDecision_DuplicateDiscriminator_UsesAnyOf()
    {
        var resolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(DuplicateDiscriminatorBase))
            {
                typeInfo.PolymorphismOptions = new()
                {
                    TypeDiscriminatorPropertyName = "kind",
                };
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new(typeof(DuplicateDiscriminatorOne), "same"));
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new(typeof(DuplicateDiscriminatorTwo), "same"));
            }
        });
        var document = BuildCompositionShape<DuplicateDiscriminatorBase>(resolver);

        var decision = document.CompositionDecisions[typeof(DuplicateDiscriminatorBase)].Alternatives;
        Assert.Equal(InferredAlternativeCompositionKind.AnyOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.DuplicateDiscriminator, decision.Reason);
        Assert.Equal(
            [typeof(DuplicateDiscriminatorOne), typeof(DuplicateDiscriminatorTwo)],
            decision.Branches.Select(branch => branch.Identity.Type));
    }

    [Fact]
    public void CompositionDecision_CSharpUnion_UsesOneOfForDisjointDomainsInDeclaredOrder()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.Strict,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        var document = InferredSchemaShapeBuilder.Build(options, typeof(ReverseShapeUnion));

        var decision = document.CompositionDecisions[typeof(ReverseShapeUnion)].Alternatives;
        Assert.Equal(InferredAlternativeSource.Union, decision.Source);
        Assert.Equal(InferredAlternativeCompositionKind.OneOf, decision.Kind);
        Assert.Equal(InferredAlternativeReason.UnionCasesHaveDisjointJsonDomains, decision.Reason);
        Assert.Equal([typeof(string), typeof(int)], decision.Branches.Select(branch => branch.Identity.Type));
        Assert.Equal(
            [InferredJsonValueDomain.String, InferredJsonValueDomain.Integer],
            decision.Branches.Select(branch => branch.JsonDomain!.Value.Domains));
    }

    [Fact]
    public void CompositionDecision_RecursiveGraph_IsFinite()
    {
        var document = BuildCompositionShape<RecursiveCompositionNode>();

        Assert.Equal(document.Shapes.Count, document.CompositionDecisions.Decisions.Count);
        Assert.Equal(
            document.Shapes.Select(shape => shape.Identity),
            document.CompositionDecisions.Decisions.Select(decision => decision.Identity));
        Assert.Equal(
            InferredAlternativeReason.NoAlternatives,
            document.CompositionDecisions[typeof(RecursiveCompositionNode)].Alternatives.Reason);
    }

    private static InferredSchemaDocument BuildCompositionShape<T>(IJsonTypeInfoResolver? resolver = null)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = resolver ?? new DefaultJsonTypeInfoResolver(),
        };
        return InferredSchemaShapeBuilder.Build(options, typeof(T));
    }

    private class SimpleBase
    {
        public string BaseValue { get; set; } = string.Empty;
    }

    private sealed class SimpleDerived : SimpleBase
    {
        public int DerivedValue { get; set; }
    }

    private class HiddenBase
    {
        [JsonPropertyName("baseValue")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class HiddenDerived : HiddenBase
    {
        [JsonPropertyName("derivedValue")]
        public new string Value { get; set; } = string.Empty;
    }

    private class CollidingBase
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class CollidingDerived : CollidingBase
    {
        public new string Value { get; set; } = string.Empty;
    }

    private class ExtensionDataBase
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Additional { get; set; } = [];
    }

    private sealed class ExtensionDataDerived : ExtensionDataBase;

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private class DisallowingBase;

    private sealed class DisallowingDerived : DisallowingBase;

    private class ConvertedBase;

    [JsonConverter(typeof(ConvertedDerivedConverter))]
    private sealed class ConvertedDerived : ConvertedBase;

    private sealed class ConvertedDerivedConverter : JsonConverter<ConvertedDerived>
    {
        public override ConvertedDerived? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new();
        }

        public override void Write(Utf8JsonWriter writer, ConvertedDerived value, JsonSerializerOptions options)
            => writer.WriteStringValue(nameof(ConvertedDerived));
    }

    private class MismatchedBase
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MismatchedDerived : MismatchedBase;

    private class UnavailableBase;

    private sealed class UnavailableBaseDerived : UnavailableBase;

    private class PlainPolymorphicAncestor;

    [JsonDerivedType(typeof(PolymorphicDerivedLeaf), "leaf")]
    private class PolymorphicDerivedRoot : PlainPolymorphicAncestor;

    private sealed class PolymorphicDerivedLeaf : PolymorphicDerivedRoot;

    private class IntermediatePolymorphicDerived : PolymorphicDerivedRoot;

    private sealed class DeepPolymorphicDerived : IntermediatePolymorphicDerived;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(OrderedZetaDerived), "zeta")]
    [JsonDerivedType(typeof(OrderedAlphaDerived), "alpha")]
    private abstract class OrderedPolymorphicBase;

    private sealed class OrderedZetaDerived : OrderedPolymorphicBase;

    private sealed class OrderedAlphaDerived : OrderedPolymorphicBase;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(DiscriminatedDerived), "known")]
    [JsonDerivedType(typeof(UndiscriminatedDerived))]
    private abstract class PartiallyDiscriminatedBase;

    private sealed class DiscriminatedDerived : PartiallyDiscriminatedBase;

    private sealed class UndiscriminatedDerived : PartiallyDiscriminatedBase;

    private abstract class DuplicateDiscriminatorBase;

    private sealed class DuplicateDiscriminatorOne : DuplicateDiscriminatorBase;

    private sealed class DuplicateDiscriminatorTwo : DuplicateDiscriminatorBase;

    private sealed class RecursiveCompositionNode
    {
        public RecursiveCompositionNode? Next { get; set; }
    }
}
