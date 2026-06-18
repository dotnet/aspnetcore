// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Verifies that C# union types survive the Server prerendering round-trip. Parameter values are
// serialized into the component marker with the runtime type of the value (the union type) and
// deserialized back by ComponentParameterDeserializer using that type with
// ServerComponentSerializationSettings.JsonSerializationOptions (the default System.Text.Json
// reflection resolver, which has native union support).
public partial class ServerComponentDeserializerTest
{
    [Fact]
    public void Union_UnambiguousIntCase_RoundTripsThroughPrerenderParameters()
    {
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionIntString(42) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionIntString(42), parameters["Value"]);
    }

    [Fact]
    public void Union_UnambiguousStringCase_RoundTripsThroughPrerenderParameters()
    {
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionIntString("hi") })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionIntString("hi"), parameters["Value"]);
    }

    [Fact]
    public void Union_NullableNullCase_RoundTripsThroughPrerenderParameters()
    {
        // A union whose active case is a null int? serializes to JSON null on the wire. The prerender
        // protocol still records a non-null type name (the union box itself is non-null), so the read
        // side restores the value by routing the JSON null literal back through the union converter
        // (ComponentParameterDeserializer special-cases JsonTypeInfoKind.Union for this).
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionNullableIntString((int?)null) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionNullableIntString((int?)null), parameters["Value"]);
    }

    [Fact]
    public void Union_NullableIntCase_RoundTripsThroughPrerenderParameters()
    {
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionNullableIntString(7) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionNullableIntString(7), parameters["Value"]);
    }

    [Fact]
    public void Union_ReferenceTypeCaseWithClassifier_RoundTripsThroughPrerenderParameters()
    {
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionPetWithClassifier(new Cat("Whiskers")) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionPetWithClassifier(new Cat("Whiskers")), parameters["Value"]);
    }

    [Fact]
    public void Union_InsideEnvelope_RoundTripsThroughPrerenderParameters()
    {
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionEnvelope("abc", new UnionIntString(42)) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
        var parameters = Assert.Single(descriptors).Parameters.ToDictionary();
        Assert.Equal(new UnionEnvelope("abc", new UnionIntString(42)), parameters["Value"]);
    }
}

// --- Test union types (kept together, mirroring SharedTypes.Unions.cs) ---

// Unambiguous primitive-paired union: int and string serialize to distinct JSON tokens.
public union UnionIntString(int, string);

// Nullable value-type case. JSON null reads back as the int? case (dotnet/runtime#128688).
public union UnionNullableIntString(int?, string);

// Reference-type cases. Both serialize to JSON Object, so the read side needs a classifier to
// disambiguate.
public record Cat(string Name);
public record Dog(string Breed);

// Classifier-disambiguated reference-type union. The classifier walks the first object member to
// identify the case ("name" -> Cat, "breed" -> Dog). Null is handled by the runtime fast-path, so
// the classifier intentionally does not branch on JsonTokenType.Null.
[JsonUnion(TypeClassifier = typeof(UnionPetClassifierFactory))]
public union UnionPetWithClassifier(Cat, Dog);

public sealed class UnionPetClassifierFactory : JsonTypeClassifierFactory<UnionPetWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) =>
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            var clone = reader;
            clone.Read();
            while (clone.TokenType == JsonTokenType.PropertyName)
            {
                if (clone.ValueTextEquals("name") || clone.ValueTextEquals("Name"))
                {
                    return typeof(Cat);
                }
                if (clone.ValueTextEquals("breed") || clone.ValueTextEquals("Breed"))
                {
                    return typeof(Dog);
                }

                clone.Read();
                clone.Skip();
                clone.Read();
            }

            return null;
        };
}

// Envelope record that holds a union as a property.
public record UnionEnvelope(string CorrelationId, UnionIntString Payload);
