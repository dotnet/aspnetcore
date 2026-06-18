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
    public void Union_NullActiveCase_IsNotSupportedByPrerenderParameters_KnownGap()
    {
        // Documents a known gap in the Server prerender parameter protocol (not a union-specific
        // serialization problem). The protocol records a parameter's type from the runtime type of
        // the boxed value: for a union holding a null case the box is non-null, so a non-null
        // TypeName/Assembly is written. But the value serializes to JSON null on the wire, and on the
        // read side ComponentParameterDeserializer unconditionally casts each parameter value to
        // JsonElement (parameterValues come back as CLR null for JSON null), which throws and fails
        // the whole descriptor. In other words the protocol conflates "value that serializes to null"
        // with "absent/null parameter". This only surfaces for types whose converter can emit null for
        // a non-null instance (such as a union whose active case is a null int? or null reference).
        // The in-process, JSInterop and PersistentComponentState paths all round-trip this case fine.
        var markers = SerializeMarkers(CreateMarkers(
            (typeof(TestComponent), new Dictionary<string, object> { ["Value"] = new UnionNullableIntString((int?)null) })));
        var serverComponentDeserializer = CreateServerComponentDeserializer();

        Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out _));
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
