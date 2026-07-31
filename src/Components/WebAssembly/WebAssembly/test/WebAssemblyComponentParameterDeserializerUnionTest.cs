// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebAssembly.Prerendering;

#nullable enable

// Verifies that C# union types survive the WebAssembly prerendering parameter round-trip. The marker
// records each parameter's runtime type and serializes its value with the default System.Text.Json
// reflection resolver (which has native union support). A union whose active case serializes to JSON
// null is the interesting case: the value comes back as a CLR null in the object-typed values array,
// and WebAssemblyComponentParameterDeserializer restores it by routing JSON null through the union
// converter for the recorded type.
public class WebAssemblyComponentParameterDeserializerUnionTest
{
    [Fact]
    public void Union_NullableNullCase_RoundTripsThroughPrerenderParameters()
    {
        var parameters = RoundTrip(new UnionNullableIntString((int?)null));

        Assert.Equal(new UnionNullableIntString((int?)null), parameters["Value"]);
    }

    [Fact]
    public void Union_NullableIntCase_RoundTripsThroughPrerenderParameters()
    {
        var parameters = RoundTrip(new UnionNullableIntString(7));

        Assert.Equal(new UnionNullableIntString(7), parameters["Value"]);
    }

    [Fact]
    public void Union_UnambiguousStringCase_RoundTripsThroughPrerenderParameters()
    {
        var parameters = RoundTrip(new UnionIntString("hi"));

        Assert.Equal(new UnionIntString("hi"), parameters["Value"]);
    }

    private static IReadOnlyDictionary<string, object?> RoundTrip<T>(T value)
    {
        var (definitions, values) = ComponentParameter.FromParameterView(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = value }));

        // Mirror the marker marshalling: parameter values are serialized to JSON and read back as an
        // object list, so a union active case that serializes to JSON null becomes a CLR null here.
        var json = JsonSerializer.Serialize(values, WebAssemblyComponentSerializationSettings.JsonSerializationOptions);
        var wireValues = WebAssemblyComponentParameterDeserializer.GetParameterValues(json);

        return WebAssemblyComponentParameterDeserializer.Instance
            .DeserializeParameters(definitions, wireValues)
            .ToDictionary();
    }
}

// --- Test union types (kept together, mirroring SharedTypes.Unions.cs) ---

// Unambiguous primitive-paired union: int and string serialize to distinct JSON tokens.
public union UnionIntString(int, string);

// Nullable value-type case. JSON null reads back as the int? case (dotnet/runtime#128688).
public union UnionNullableIntString(int?, string);
