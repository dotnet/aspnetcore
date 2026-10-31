// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class JsonHubProtocolUnionTests
{
    private readonly IHubProtocol _protocol = new JsonHubProtocol();

    // --- Invocation message (hub method parameter) ---

    [Fact]
    public void UnionAsInvocationArgument_UnambiguousIntCase_RoundTrips()
    {
        var expectedJson = "{\"type\":1,\"target\":\"Method\",\"arguments\":[42]}";

        var written = WriteAsString(new InvocationMessage("Method", new object[] { new UnionIntString(42) }));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseInvocation(written, paramType: typeof(UnionIntString));
        var arg = Assert.IsType<UnionIntString>(Assert.Single(parsed.Arguments!));
        Assert.Equal(new UnionIntString(42), arg);
    }

    [Fact]
    public void UnionAsInvocationArgument_UnambiguousStringCase_RoundTrips()
    {
        var expectedJson = "{\"type\":1,\"target\":\"Method\",\"arguments\":[\"hi\"]}";

        var written = WriteAsString(new InvocationMessage("Method", new object[] { new UnionIntString("hi") }));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseInvocation(written, paramType: typeof(UnionIntString));
        var arg = Assert.IsType<UnionIntString>(Assert.Single(parsed.Arguments!));
        Assert.Equal(new UnionIntString("hi"), arg);
    }

    [Fact]
    public void UnionAsInvocationArgument_NullableIntCase_NullRoutesToNullableCase()
    {
        var json = Frame("{\"type\":1,\"target\":\"Method\",\"arguments\":[null]}");

        var parsed = ParseInvocation(json, paramType: typeof(UnionNullableIntString));
        var arg = Assert.IsType<UnionNullableIntString>(Assert.Single(parsed.Arguments!));
        Assert.Equal(new UnionNullableIntString((int?)null), arg);
    }

    [Fact]
    public void UnionAsInvocationArgument_NullableValueCase_RoundTrips()
    {
        var expectedJson = "{\"type\":1,\"target\":\"Method\",\"arguments\":[5]}";

        var written = WriteAsString(new InvocationMessage("Method", new object[] { new UnionNullableIntString((int?)5) }));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseInvocation(written, paramType: typeof(UnionNullableIntString));
        var arg = Assert.IsType<UnionNullableIntString>(Assert.Single(parsed.Arguments!));
        Assert.Equal(new UnionNullableIntString((int?)5), arg);
    }

    [Fact]
    public void UnionAsInvocationArgument_ClassifierResolvesAmbiguousObjectCase()
    {
        // UnionPet(Cat, Dog) — both cases serialize to JSON Object, so without a classifier the
        // read side is ambiguous. UnionPetWithClassifier opts in to a classifier that dispatches
        // by property name.
        var json = Frame("{\"type\":1,\"target\":\"Method\",\"arguments\":[{\"name\":\"Whiskers\"}]}");

        var parsed = ParseInvocation(json, paramType: typeof(UnionPetWithClassifier));
        var arg = Assert.IsType<UnionPetWithClassifier>(Assert.Single(parsed.Arguments!));
        Assert.Equal(new UnionPetWithClassifier(new Cat("Whiskers")), arg);
    }

    [Fact]
    public void UnionAsInvocationArgument_MixedWithNonUnionArguments_RoundTrips()
    {
        // Hub method signature: SendMessage(string topic, UnionIntString payload, int seq).
        var message = new InvocationMessage("Method", new object[] { "topic", new UnionIntString(42), 7 });

        var written = WriteAsString(message);
        Assert.Equal(Frame("{\"type\":1,\"target\":\"Method\",\"arguments\":[\"topic\",42,7]}"), written);

        var binder = new TestBinder(new[] { typeof(string), typeof(UnionIntString), typeof(int) });
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(written));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var parsed));

        var invocation = Assert.IsType<InvocationMessage>(parsed);
        Assert.Equal("topic", invocation.Arguments![0]);
        Assert.Equal(new UnionIntString(42), Assert.IsType<UnionIntString>(invocation.Arguments[1]));
        Assert.Equal(7, invocation.Arguments[2]);
    }

    // --- Completion message (hub method return value) ---

    [Fact]
    public void UnionAsCompletionResult_UnambiguousIntCase_RoundTrips()
    {
        var expectedJson = "{\"type\":3,\"invocationId\":\"1\",\"result\":42}";

        var written = WriteAsString(CompletionMessage.WithResult("1", new UnionIntString(42)));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseCompletion(written, returnType: typeof(UnionIntString));
        Assert.Equal(new UnionIntString(42), Assert.IsType<UnionIntString>(parsed.Result));
    }

    [Fact]
    public void UnionAsCompletionResult_UnambiguousStringCase_RoundTrips()
    {
        var expectedJson = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"hi\"}";

        var written = WriteAsString(CompletionMessage.WithResult("1", new UnionIntString("hi")));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseCompletion(written, returnType: typeof(UnionIntString));
        Assert.Equal(new UnionIntString("hi"), Assert.IsType<UnionIntString>(parsed.Result));
    }

    [Fact]
    public void UnionAsCompletionResult_NullableValueCase_RoundTripsAsNullLiteral()
    {
        // The active case is int? carrying a null value. The union converter writes the case's
        // own representation (the JSON null literal), and on read the runtime fast-path routes
        // null back to the nullable case.
        var expectedJson = "{\"type\":3,\"invocationId\":\"1\",\"result\":null}";

        var written = WriteAsString(CompletionMessage.WithResult("1", new UnionNullableIntString((int?)null)));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseCompletion(written, returnType: typeof(UnionNullableIntString));
        Assert.Equal(new UnionNullableIntString((int?)null), Assert.IsType<UnionNullableIntString>(parsed.Result));
    }

    [Fact]
    public void UnionAsCompletionResult_ObjectCase_SerializesActiveCaseOnly()
    {
        // Write-side dispatch uses the runtime case (Cat / Dog) — no union envelope on the wire.
        var catJson = WriteAsString(CompletionMessage.WithResult("1", new UnionPet(new Cat("Whiskers"))));
        Assert.Equal(Frame("{\"type\":3,\"invocationId\":\"1\",\"result\":{\"name\":\"Whiskers\"}}"), catJson);

        var dogJson = WriteAsString(CompletionMessage.WithResult("2", new UnionPet(new Dog("Labrador"))));
        Assert.Equal(Frame("{\"type\":3,\"invocationId\":\"2\",\"result\":{\"breed\":\"Labrador\"}}"), dogJson);
    }

    [Fact]
    public void UnionAsCompletionResult_ClassifierResolvesAmbiguousObjectCase_OnRead()
    {
        var dogJson = Frame("{\"type\":3,\"invocationId\":\"1\",\"result\":{\"breed\":\"Labrador\"}}");

        var parsed = ParseCompletion(dogJson, returnType: typeof(UnionPetWithClassifier));
        Assert.Equal(new UnionPetWithClassifier(new Dog("Labrador")), Assert.IsType<UnionPetWithClassifier>(parsed.Result));
    }

    // --- Stream item message (server -> client streaming) ---
    // SignalR translates server-side IAsyncEnumerable<T>/ChannelReader<T> hub method returns into
    // a sequence of StreamItemMessage envelopes, one per yielded element, each carrying the
    // element-typed value. So end-to-end IAsyncEnumerable<Union>/ChannelReader<Union> support
    // reduces at the protocol layer to: does a single union value round-trip as a stream item.

    [Fact]
    public void UnionAsStreamItem_UnambiguousIntCase_RoundTrips()
    {
        var expectedJson = "{\"type\":2,\"invocationId\":\"1\",\"item\":42}";

        var written = WriteAsString(new StreamItemMessage("1", new UnionIntString(42)));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseStreamItem(written, streamItemType: typeof(UnionIntString));
        Assert.Equal(new UnionIntString(42), Assert.IsType<UnionIntString>(parsed.Item));
    }

    [Fact]
    public void UnionAsStreamItem_UnambiguousStringCase_RoundTrips()
    {
        var expectedJson = "{\"type\":2,\"invocationId\":\"1\",\"item\":\"hi\"}";

        var written = WriteAsString(new StreamItemMessage("1", new UnionIntString("hi")));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseStreamItem(written, streamItemType: typeof(UnionIntString));
        Assert.Equal(new UnionIntString("hi"), Assert.IsType<UnionIntString>(parsed.Item));
    }

    [Fact]
    public void UnionAsStreamItem_ObjectCaseWithClassifier_RoundTrips()
    {
        var expectedJson = "{\"type\":2,\"invocationId\":\"1\",\"item\":{\"name\":\"Whiskers\"}}";

        var written = WriteAsString(new StreamItemMessage("1", new UnionPetWithClassifier(new Cat("Whiskers"))));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseStreamItem(written, streamItemType: typeof(UnionPetWithClassifier));
        Assert.Equal(new UnionPetWithClassifier(new Cat("Whiskers")), Assert.IsType<UnionPetWithClassifier>(parsed.Item));
    }

    // --- Stream invocation (client -> server streaming arg) ---

    [Fact]
    public void UnionAsStreamInvocationArgument_RoundTrips()
    {
        var expectedJson = "{\"type\":4,\"invocationId\":\"1\",\"target\":\"Method\",\"arguments\":[42]}";

        var written = WriteAsString(new StreamInvocationMessage("1", "Method", new object[] { new UnionIntString(42) }));
        Assert.Equal(Frame(expectedJson), written);

        var binder = new TestBinder(new[] { typeof(UnionIntString) });
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(written));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var parsed));

        var streamInvocation = Assert.IsType<StreamInvocationMessage>(parsed);
        Assert.Equal(new UnionIntString(42), Assert.IsType<UnionIntString>(Assert.Single(streamInvocation.Arguments!)));
    }

    // --- Envelope: union nested inside a non-union model carried as a hub method argument. ---

    [Fact]
    public void UnionInsideEnvelopeMessage_RoundTrips()
    {
        var expectedJson = "{\"type\":1,\"target\":\"Method\",\"arguments\":[{\"correlationId\":\"abc\",\"payload\":42}]}";

        var envelope = new UnionEnvelope("abc", new UnionIntString(42));
        var written = WriteAsString(new InvocationMessage("Method", new object[] { envelope }));
        Assert.Equal(Frame(expectedJson), written);

        var parsed = ParseInvocation(written, paramType: typeof(UnionEnvelope));
        var arg = Assert.IsType<UnionEnvelope>(Assert.Single(parsed.Arguments!));
        Assert.Equal(envelope, arg);
    }

    // --- Argument binding errors should surface as InvocationBindingFailureMessage ---

    [Fact]
    public void UnionAsInvocationArgument_TokenNotMatchingAnyCase_ProducesBindingFailure()
    {
        // UnionIntString(int, string) — a JSON Boolean token matches neither case and has no classifier.
        var json = Frame("{\"type\":1,\"invocationId\":\"42\",\"target\":\"Method\",\"arguments\":[true]}");

        var binder = new TestBinder(new[] { typeof(UnionIntString) });
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var parsed));

        var failure = Assert.IsType<InvocationBindingFailureMessage>(parsed);
        Assert.Equal("Method", failure.Target);
    }

    // --- Helpers ---

    private string WriteAsString(HubMessage message)
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            _protocol.WriteMessage(message, writer);
            return Encoding.UTF8.GetString(writer.ToArray());
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    private InvocationMessage ParseInvocation(string framedJson, Type paramType)
    {
        var binder = new TestBinder(new[] { paramType });
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(framedJson));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var message));

        return Assert.IsType<InvocationMessage>(message);
    }

    private CompletionMessage ParseCompletion(string framedJson, Type returnType)
    {
        var binder = new TestBinder(returnType);
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(framedJson));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var message));

        return Assert.IsType<CompletionMessage>(message);
    }

    private StreamItemMessage ParseStreamItem(string framedJson, Type streamItemType)
    {
        var binder = new TestBinder(streamItemType);
        var data = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(framedJson));
        Assert.True(_protocol.TryParseMessage(ref data, binder, out var message));

        return Assert.IsType<StreamItemMessage>(message);
    }

    private static string Frame(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var framed = new byte[bytes.Length + 1];
        bytes.CopyTo(framed, 0);
        framed[^1] = TextMessageFormatter.RecordSeparator;
        return Encoding.UTF8.GetString(framed);
    }
}

// --- Test union types ---

// Unambiguous primitive-paired union: int and string serialize to distinct JSON tokens
// (Number and String), so no classifier is needed for round-tripping.
public union UnionIntString(int, string);

// Nullable value-type case. With dotnet/runtime#128688, int and int? are tracked as separate
// union cases, and the runtime fast-path dispatches JSON null to the nullable case
// deterministically — so a JSON `null` reads back as UnionNullableIntString((int?)null).
public union UnionNullableIntString(int?, string);

// Reference-type cases. Both Cat and Dog serialize to JSON Object, so the read side is
// implicitly ambiguous without a classifier. Write side dispatches by runtime type.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Classifier-disambiguated variant of UnionPet. The classifier walks the first object
// member to find a property that identifies the case ("name" → Cat, "breed" → Dog).
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

// Envelope record that holds a union as a property — verifies unions work when nested
// inside another type carried as a hub method argument.
public record UnionEnvelope(string CorrelationId, UnionIntString Payload);
