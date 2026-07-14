// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

// Verifies that C# union types round-trip through the [JSInvokable] dispatch path (JS -> .NET).
// DotNetDispatcher deserializes method arguments and serializes the return value using the
// JSRuntime's JsonSerializerOptions, so union support flows from System.Text.Json.
public class DotNetDispatcherUnionTests
{
    private static readonly string thisAssemblyName = typeof(DotNetDispatcherUnionTests).Assembly.GetName().Name;

    [Fact]
    public void UnionParameterAndReturn_UnambiguousIntCase_RoundTrips()
    {
        var resultJson = Invoke("EchoUnionIntString", "[42]");

        Assert.Equal("42", resultJson);
        Assert.Equal(new UnionIntString(42), Deserialize<UnionIntString>(resultJson));
    }

    [Fact]
    public void UnionParameterAndReturn_UnambiguousStringCase_RoundTrips()
    {
        var resultJson = Invoke("EchoUnionIntString", "[\"hi\"]");

        Assert.Equal("\"hi\"", resultJson);
        Assert.Equal(new UnionIntString("hi"), Deserialize<UnionIntString>(resultJson));
    }

    [Fact]
    public void UnionParameterAndReturn_NullableNullCase_RoutesToNullableCase()
    {
        var resultJson = Invoke("EchoUnionNullableIntString", "[null]");

        Assert.Equal("null", resultJson);
        Assert.Equal(new UnionNullableIntString((int?)null), Deserialize<UnionNullableIntString>(resultJson));
    }

    [Fact]
    public void UnionReturn_ObjectCase_SerializesActiveCaseOnly()
    {
        Assert.Equal("{\"name\":\"Whiskers\"}", Invoke("ReturnCat", null));
        Assert.Equal("{\"breed\":\"Labrador\"}", Invoke("ReturnDog", null));
    }

    [Fact]
    public void UnionParameterAndReturn_ClassifierResolvesAmbiguousObjectCase()
    {
        var resultJson = Invoke("EchoUnionPetWithClassifier", "[{\"name\":\"Whiskers\"}]");

        Assert.Equal(new UnionPetWithClassifier(new Cat("Whiskers")), Deserialize<UnionPetWithClassifier>(resultJson));
    }

    [Fact]
    public void UnionInsideEnvelopeParameter_RoundTrips()
    {
        var resultJson = Invoke("EchoUnionEnvelope", "[{\"correlationId\":\"abc\",\"payload\":42}]");

        Assert.Equal(new UnionEnvelope("abc", new UnionIntString(42)), Deserialize<UnionEnvelope>(resultJson));
    }

    private static string Invoke(string methodIdentifier, string argsJson)
        => DotNetDispatcher.Invoke(new TestJSRuntime(), new DotNetInvocationInfo(thisAssemblyName, methodIdentifier, default, default), argsJson);

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, new TestJSRuntime().JsonSerializerOptions);
}

public static class UnionJSInvokableTarget
{
    [JSInvokable("EchoUnionIntString")]
    public static UnionIntString EchoUnionIntString(UnionIntString value) => value;

    [JSInvokable("EchoUnionNullableIntString")]
    public static UnionNullableIntString EchoUnionNullableIntString(UnionNullableIntString value) => value;

    [JSInvokable("EchoUnionPetWithClassifier")]
    public static UnionPetWithClassifier EchoUnionPetWithClassifier(UnionPetWithClassifier value) => value;

    [JSInvokable("EchoUnionEnvelope")]
    public static UnionEnvelope EchoUnionEnvelope(UnionEnvelope value) => value;

    [JSInvokable("ReturnCat")]
    public static UnionPet ReturnCat() => new UnionPet(new Cat("Whiskers"));

    [JSInvokable("ReturnDog")]
    public static UnionPet ReturnDog() => new UnionPet(new Dog("Labrador"));
}
