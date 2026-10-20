// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

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

    [Fact]
    public void NullParameter_RoundTripsThroughPrerenderParameters()
    {
        var parameters = RoundTrip<string?>(null);

        Assert.Null(parameters["Value"]);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int?))]
    public void DeserializeParameters_RejectsTypedNullForNonUnion(Type parameterType)
    {
        var definition = new ComponentParameter
        {
            Name = "Value",
            TypeName = parameterType.FullName,
            Assembly = parameterType.Assembly.GetName().Name,
        };
        var wireValues = WebAssemblyComponentParameterDeserializer.GetParameterValues("[null]");
        var deserializer = new WebAssemblyComponentParameterDeserializer(
            new ComponentParametersTypeCache(),
            WebAssemblyComponentSerializationSettings.CreateOptions());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            deserializer.DeserializeParameters([definition], wireValues));

        Assert.Equal(
            $"Could not parse the parameter value for parameter '{definition.Name}' of type '{definition.TypeName}' and assembly '{definition.Assembly}'.",
            exception.Message);
    }

    private static IReadOnlyDictionary<string, object?> RoundTrip<T>(T value)
    {
        var (definitions, values) = ComponentParameter.FromParameterView(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = value }));

        // Prerendering and WebAssembly run in separate runtimes. Keep both options fresh and separate
        // so neither producer serialization nor another test can initialize the reader's options.
        var producerOptions = WebAssemblyComponentSerializationSettings.CreateOptions();
        var readerOptions = WebAssemblyComponentSerializationSettings.CreateOptions();
        var deserializer = new WebAssemblyComponentParameterDeserializer(
            new ComponentParametersTypeCache(),
            readerOptions);
        var json = JsonSerializer.Serialize(values, producerOptions);
        var wireValues = WebAssemblyComponentParameterDeserializer.GetParameterValues(json);

        return deserializer
            .DeserializeParameters(definitions, wireValues)
            .ToDictionary();
    }
}

// Unambiguous primitive-paired union: int and string serialize to distinct JSON tokens.
public union UnionIntString(int, string);

// Nullable value-type case. JSON null reads back as the int? case (dotnet/runtime#128688).
public union UnionNullableIntString(int?, string);
