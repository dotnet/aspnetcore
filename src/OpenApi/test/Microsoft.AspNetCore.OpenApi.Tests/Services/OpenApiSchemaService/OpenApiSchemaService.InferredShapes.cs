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
    public void InferredShape_RecursiveType_IsFiniteAndCanonical()
    {
        var document = BuildShape<RecursiveNode>();

        var node = document[typeof(RecursiveNode)];
        Assert.Equal(InferredSchemaShapeKind.Object, node.Kind);
        Assert.False(document.Root.AllowsNull);
        Assert.Equal(["children", "next", "value"], node.Properties.Select(property => property.Identity.JsonName));
        Assert.Equal(typeof(RecursiveNode), node.GetProperty("next").PropertyType.Identity.Type);
        Assert.True(node.GetProperty("next").PropertyType.AllowsNull);
        Assert.Equal(typeof(RecursiveNode), document[typeof(List<RecursiveNode>)].ElementType?.Identity.Type);
        Assert.False(document[typeof(List<RecursiveNode>)].ElementType?.AllowsNull);
        Assert.Equal(document.Shapes.Count, document.Shapes.Select(shape => shape.Identity).Distinct().Count());
    }

    [Fact]
    public void InferredShape_NullableTypeUse_DoesNotMutateCanonicalShape()
    {
        var document = BuildShape<NullableContainer>();

        var container = document[typeof(NullableContainer)];
        Assert.Equal(typeof(NamedComponent), container.GetProperty("component").PropertyType.Identity.Type);
        Assert.True(container.GetProperty("component").PropertyType.AllowsNull);
        Assert.False(document[typeof(NamedComponent)].Identity.Type.IsValueType);
        Assert.Single(document.Shapes.Where(shape => shape.Identity.Type == typeof(NamedComponent)));

        var nullableValueDocument = BuildShape<int?>();
        Assert.Equal(typeof(int), nullableValueDocument.Root.Identity.Type);
        Assert.True(nullableValueDocument.Root.AllowsNull);
    }

    [Fact]
    public void InferredShape_PropertyNullability_UsesGetterOrSetterContract()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(AsymmetricNullabilityContainer))
                {
                    var property = Assert.Single(typeInfo.Properties);
                    property.IsGetNullable = false;
                    property.IsSetNullable = true;
                }
            }),
        };

        var document = InferredSchemaShapeBuilder.Build(options, typeof(AsymmetricNullabilityContainer));

        Assert.True(document[typeof(AsymmetricNullabilityContainer)].GetProperty("value").PropertyType.AllowsNull);
    }

    [Fact]
    public void InferredShape_Polymorphism_PreservesConfiguredOrderAndClosesBaseGraph()
    {
        var document = BuildShape<PolymorphicBase>();

        var baseShape = document[typeof(PolymorphicBase)];
        Assert.Equal(
            [typeof(ZetaDerived), typeof(AlphaDerived)],
            baseShape.DerivedTypes.Select(derivedType => derivedType.Identity.Type));
        Assert.Equal(["zeta", "alpha"], baseShape.DerivedTypes.Select(derivedType => derivedType.Discriminator));
        Assert.Equal(typeof(PolymorphicBase), document[typeof(AlphaDerived)].BaseType?.Type);
        Assert.Equal(typeof(PolymorphicBase), document[typeof(ZetaDerived)].BaseType?.Type);

        var derivedDocument = BuildShape<AlphaDerived>();
        Assert.Equal(InferredSchemaShapeKind.Object, derivedDocument[typeof(PolymorphicBase)].Kind);
        Assert.Equal(derivedDocument.Shapes.Count, derivedDocument.Shapes.Select(shape => shape.Identity).Distinct().Count());
    }

    [Fact]
    public void InferredShape_Union_PreservesDeclaredCaseOrder()
    {
        var document = BuildShape<ReverseShapeUnion>();

        var union = document[typeof(ReverseShapeUnion)];
        Assert.Equal(InferredSchemaShapeKind.Union, union.Kind);
        Assert.Equal([typeof(string), typeof(int)], union.UnionCases.Select(unionCase => unionCase.Identity.Type));
    }

    [Fact]
    public void InferredShape_DictionaryAndExtensionData_PreserveNamedProperties()
    {
        var dictionaryDocument = BuildShape<Dictionary<string, NamedComponent>>();
        var dictionary = dictionaryDocument[typeof(Dictionary<string, NamedComponent>)];
        Assert.Equal(InferredSchemaShapeKind.Dictionary, dictionary.Kind);
        Assert.Equal(typeof(NamedComponent), dictionary.AdditionalPropertiesType?.Identity.Type);

        var extensionDataDocument = BuildShape<ExtensionDataContainer>();
        var container = extensionDataDocument[typeof(ExtensionDataContainer)];
        Assert.Equal(["name"], container.Properties.Select(property => property.Identity.JsonName));
        Assert.True(container.ExtensionDataProperty?.IsExtensionData);
        Assert.Equal(typeof(JsonElement), container.AdditionalPropertiesType?.Identity.Type);
    }

    [Fact]
    public void InferredShape_GenericNestedAndCustomConverter_HaveStableIdentityAndCoreShape()
    {
        var genericDocument = BuildShape<GenericContainer<NestedShapeType>>();
        Assert.Equal(typeof(GenericContainer<NestedShapeType>), genericDocument.Root.Identity.Type);
        Assert.Contains('+', genericDocument[typeof(NestedShapeType)].Identity.Name);
        Assert.Equal(typeof(NestedShapeType), genericDocument[typeof(GenericContainer<NestedShapeType>)].GetProperty("item").PropertyType.Identity.Type);

        var converterDocument = BuildShape<ConvertedValue>();
        var convertedValue = converterDocument[typeof(ConvertedValue)];
        Assert.Equal(InferredSchemaShapeKind.Scalar, convertedValue.Kind);
        Assert.Equal(typeof(ConvertedValueConverter), convertedValue.ConverterType);
        Assert.True(convertedValue.HasCustomConverter);
        Assert.Empty(convertedValue.Properties);
    }

    private static InferredSchemaDocument BuildShape<T>()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        return InferredSchemaShapeBuilder.Build(options, typeof(T));
    }

    private sealed class RecursiveNode
    {
        public int Value { get; set; }

        public RecursiveNode? Next { get; set; }

        public List<RecursiveNode> Children { get; set; } = [];
    }

    private sealed class NullableContainer
    {
        public NamedComponent? Component { get; set; }
    }

    private sealed class AsymmetricNullabilityContainer
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class NamedComponent
    {
        public string Name { get; set; } = string.Empty;
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(ZetaDerived), "zeta")]
    [JsonDerivedType(typeof(AlphaDerived), "alpha")]
    private abstract class PolymorphicBase;

    private sealed class ZetaDerived : PolymorphicBase;

    private sealed class AlphaDerived : PolymorphicBase;

    private sealed class ExtensionDataContainer
    {
        public string Name { get; set; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> Additional { get; set; } = [];
    }

    private sealed class GenericContainer<T>
    {
        public required T Item { get; set; }
    }

    private sealed class NestedShapeType;

    [JsonConverter(typeof(ConvertedValueConverter))]
    private sealed class ConvertedValue;

    private sealed class ConvertedValueConverter : JsonConverter<ConvertedValue>
    {
        public override ConvertedValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new();
        }

        public override void Write(Utf8JsonWriter writer, ConvertedValue value, JsonSerializerOptions options)
            => writer.WriteStringValue(nameof(ConvertedValue));
    }
}

internal union ReverseShapeUnion(string, int);
