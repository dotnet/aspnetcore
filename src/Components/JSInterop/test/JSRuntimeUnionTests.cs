// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

// Verifies that C# union types round-trip through JSInterop, which serializes interop
// arguments and deserializes results with the JSRuntime's JsonSerializerOptions (the default
// System.Text.Json reflection resolver, which has native union support).
public class JSRuntimeUnionTests
{
    // --- Argument serialization (.NET -> JS): a union passed to InvokeAsync is written using
    // the active case's own JSON representation, with no union envelope on the wire. ---

    [Fact]
    public void UnionAsArgument_UnambiguousIntCase_SerializesAsNumber()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", new UnionIntString(42));

        Assert.Equal("[42]", runtime.BeginInvokeCalls.Single().ArgsJson);
    }

    [Fact]
    public void UnionAsArgument_UnambiguousStringCase_SerializesAsString()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", new UnionIntString("hi"));

        Assert.Equal("[\"hi\"]", runtime.BeginInvokeCalls.Single().ArgsJson);
    }

    [Fact]
    public void UnionAsArgument_NullableNullCase_SerializesAsNullLiteral()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", new UnionNullableIntString((int?)null));

        Assert.Equal("[null]", runtime.BeginInvokeCalls.Single().ArgsJson);
    }

    [Fact]
    public void UnionAsArgument_ObjectCase_SerializesActiveCaseOnly()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", new UnionPet(new Cat("Whiskers")));
        Assert.Equal("[{\"name\":\"Whiskers\"}]", runtime.BeginInvokeCalls.Single().ArgsJson);

        var dogRuntime = new CapturingJSRuntime();
        dogRuntime.InvokeAsync<object>("identifier", new UnionPet(new Dog("Labrador")));
        Assert.Equal("[{\"breed\":\"Labrador\"}]", dogRuntime.BeginInvokeCalls.Single().ArgsJson);
    }

    [Fact]
    public void UnionInsideEnvelope_SerializesNestedUnion()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", new UnionEnvelope("abc", new UnionIntString(42)));

        Assert.Equal("[{\"correlationId\":\"abc\",\"payload\":42}]", runtime.BeginInvokeCalls.Single().ArgsJson);
    }

    [Fact]
    public void UnionMixedWithNonUnionArguments_SerializesPositionally()
    {
        var runtime = new CapturingJSRuntime();

        runtime.InvokeAsync<object>("identifier", "topic", new UnionIntString(42), 7);

        Assert.Equal("[\"topic\",42,7]", runtime.BeginInvokeCalls.Single().ArgsJson);
    }

    // --- Result deserialization (JS -> .NET): a union returned from InvokeAsync<T> is read back
    // from the active case's JSON representation. ---

    [Fact]
    public async Task UnionAsResult_UnambiguousIntCase_RoundTrips()
    {
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionIntString>("identifier");

        CompleteWithJson(runtime, "42");

        Assert.Equal(new UnionIntString(42), await task);
    }

    [Fact]
    public async Task UnionAsResult_UnambiguousStringCase_RoundTrips()
    {
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionIntString>("identifier");

        CompleteWithJson(runtime, "\"hi\"");

        Assert.Equal(new UnionIntString("hi"), await task);
    }

    [Fact]
    public async Task UnionAsResult_NullableNullCase_RoutesToNullableCase()
    {
        // With dotnet/runtime#128688 the runtime tracks int and int? as separate cases and a
        // value-based fast-path dispatches JSON null to the nullable case deterministically.
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionNullableIntString>("identifier");

        CompleteWithJson(runtime, "null");

        Assert.Equal(new UnionNullableIntString((int?)null), await task);
    }

    [Fact]
    public async Task UnionAsResult_NullableValueCase_RoundTrips()
    {
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionNullableIntString>("identifier");

        CompleteWithJson(runtime, "5");

        Assert.Equal(new UnionNullableIntString((int?)5), await task);
    }

    [Fact]
    public async Task UnionAsResult_ClassifierResolvesAmbiguousObjectCase()
    {
        // Cat and Dog both serialize to a JSON object, so the read side needs a classifier to
        // disambiguate. UnionPetWithClassifier dispatches by property name.
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionPetWithClassifier>("identifier");

        CompleteWithJson(runtime, "{\"name\":\"Whiskers\"}");

        Assert.Equal(new UnionPetWithClassifier(new Cat("Whiskers")), await task);
    }

    [Fact]
    public async Task UnionInsideEnvelope_AsResult_RoundTrips()
    {
        var runtime = new CapturingJSRuntime();
        var task = runtime.InvokeAsync<UnionEnvelope>("identifier");

        CompleteWithJson(runtime, "{\"correlationId\":\"abc\",\"payload\":42}");

        Assert.Equal(new UnionEnvelope("abc", new UnionIntString(42)), await task);
    }

    private static void CompleteWithJson(CapturingJSRuntime runtime, string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        runtime.EndInvokeJS(runtime.BeginInvokeCalls.Single().AsyncHandle, succeeded: true, ref reader);
    }

    private sealed class CapturingJSRuntime : JSRuntime
    {
        public List<JSInvocationInfo> BeginInvokeCalls { get; } = [];

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException();

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
            => BeginInvokeCalls.Add(invocationInfo);

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotImplementedException();

        protected internal override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
            => Task.CompletedTask;
    }
}

// --- Test union types (kept together, mirroring SharedTypes.Unions.cs) ---

// Unambiguous primitive-paired union: int and string serialize to distinct JSON tokens.
public union UnionIntString(int, string);

// Nullable value-type case. JSON null reads back as the int? case (dotnet/runtime#128688).
public union UnionNullableIntString(int?, string);

// Reference-type cases. Both serialize to JSON Object, so the read side is ambiguous without a
// classifier; the write side dispatches by runtime type.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Classifier-disambiguated variant of UnionPet. The classifier walks the first object member to
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
