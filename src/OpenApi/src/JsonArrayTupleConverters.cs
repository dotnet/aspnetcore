// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Creates reflection-free JSON converters for closed <see cref="Tuple"/> and <see cref="ValueTuple"/> types.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class JsonArrayTupleConverters
{
    /// <summary>
    /// Creates a converter for <see cref="ValueTuple"/>.
    /// </summary>
    public static JsonConverter<ValueTuple> CreateValueTuple()
        => Create<ValueTuple>(
            [],
            static (ref Utf8JsonReader _, JsonSerializerOptions _) => default,
            static (Utf8JsonWriter _, ValueTuple _, JsonSerializerOptions _) => { });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1>> CreateValueTuple<T1>()
        => Create<ValueTuple<T1>>(
            [typeof(T1)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(ReadElement<T1>(ref reader, options)),
            static (writer, value, options) =>
                WriteElement(writer, value.Item1, options));

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2>> CreateValueTuple<T1, T2>()
        => Create<ValueTuple<T1, T2>>(
            [typeof(T1), typeof(T2)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2, T3}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2, T3>> CreateValueTuple<T1, T2, T3>()
        => Create<ValueTuple<T1, T2, T3>>(
            [typeof(T1), typeof(T2), typeof(T3)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2, T3, T4}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2, T3, T4>> CreateValueTuple<T1, T2, T3, T4>()
        => Create<ValueTuple<T1, T2, T3, T4>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2, T3, T4, T5>> CreateValueTuple<T1, T2, T3, T4, T5>()
        => Create<ValueTuple<T1, T2, T3, T4, T5>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6>> CreateValueTuple<T1, T2, T3, T4, T5, T6>()
        => Create<ValueTuple<T1, T2, T3, T4, T5, T6>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    public static JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> CreateValueTuple<T1, T2, T3, T4, T5, T6, T7>()
        => Create<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options),
                    ReadElement<T7>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
                WriteElement(writer, value.Item7, options);
            });

    /// <summary>
    /// Creates a converter for an eight-parameter <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7, TRest}"/>
    /// that flattens the supplied converter for <typeparamref name="TRest"/>.
    /// </summary>
    /// <param name="restConverter">The reflection-free converter for the tuple stored in <c>Rest</c>.</param>
    public static JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>> CreateValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
        JsonConverter<TRest> restConverter)
        where TRest : struct, ITuple
    {
        var restCodec = GetRestCodec(restConverter);
        return Create<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>(
            CombineElementTypes([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)], restCodec.ElementTypes),
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options),
                    ReadElement<T7>(ref reader, options),
                    restCodec.ReadElements(ref reader, options)),
            (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
                WriteElement(writer, value.Item7, options);
                restCodec.WriteElements(writer, value.Rest, options);
            });
    }

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1>> CreateTuple<T1>()
        => Create<Tuple<T1>>(
            [typeof(T1)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(ReadElement<T1>(ref reader, options)),
            static (writer, value, options) =>
                WriteElement(writer, value.Item1, options));

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2>> CreateTuple<T1, T2>()
        => Create<Tuple<T1, T2>>(
            [typeof(T1), typeof(T2)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2, T3}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2, T3>> CreateTuple<T1, T2, T3>()
        => Create<Tuple<T1, T2, T3>>(
            [typeof(T1), typeof(T2), typeof(T3)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2, T3, T4}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2, T3, T4>> CreateTuple<T1, T2, T3, T4>()
        => Create<Tuple<T1, T2, T3, T4>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2, T3, T4, T5>> CreateTuple<T1, T2, T3, T4, T5>()
        => Create<Tuple<T1, T2, T3, T4, T5>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6>> CreateTuple<T1, T2, T3, T4, T5, T6>()
        => Create<Tuple<T1, T2, T3, T4, T5, T6>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
            });

    /// <summary>
    /// Creates a converter for <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6, T7>> CreateTuple<T1, T2, T3, T4, T5, T6, T7>()
        => Create<Tuple<T1, T2, T3, T4, T5, T6, T7>>(
            [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)],
            static (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options),
                    ReadElement<T7>(ref reader, options)),
            static (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
                WriteElement(writer, value.Item7, options);
            });

    /// <summary>
    /// Creates a converter for an eight-parameter <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7, TRest}"/>
    /// that flattens the supplied converter for <typeparamref name="TRest"/>.
    /// </summary>
    /// <param name="restConverter">The reflection-free converter for the tuple stored in <c>Rest</c>.</param>
    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>> CreateTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
        JsonConverter<TRest> restConverter)
        where TRest : ITuple
    {
        var restCodec = GetRestCodec(restConverter);
        return Create<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>(
            CombineElementTypes([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)], restCodec.ElementTypes),
            (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
                new(
                    ReadElement<T1>(ref reader, options),
                    ReadElement<T2>(ref reader, options),
                    ReadElement<T3>(ref reader, options),
                    ReadElement<T4>(ref reader, options),
                    ReadElement<T5>(ref reader, options),
                    ReadElement<T6>(ref reader, options),
                    ReadElement<T7>(ref reader, options),
                    restCodec.ReadElements(ref reader, options)),
            (writer, value, options) =>
            {
                WriteElement(writer, value.Item1, options);
                WriteElement(writer, value.Item2, options);
                WriteElement(writer, value.Item3, options);
                WriteElement(writer, value.Item4, options);
                WriteElement(writer, value.Item5, options);
                WriteElement(writer, value.Item6, options);
                WriteElement(writer, value.Item7, options);
                restCodec.WriteElements(writer, value.Rest, options);
            });
    }

    private static JsonConverter<TTuple> Create<TTuple>(
        Type[] elementTypes,
        JsonArrayTupleReadElements<TTuple> readElements,
        JsonArrayTupleWriteElements<TTuple> writeElements)
    {
        var contract = JsonArrayTupleContract.Create(typeof(TTuple), Array.AsReadOnly(elementTypes));
        var codec = new JsonArrayTupleCodec<TTuple>(contract.ElementTypes, readElements, writeElements);
        return new ClosedJsonArrayTupleConverter<TTuple>(contract, codec);
    }

    private static JsonArrayTupleCodec<TRest> GetRestCodec<TRest>(JsonConverter<TRest> restConverter)
        where TRest : ITuple
    {
        ArgumentNullException.ThrowIfNull(restConverter);
        if (restConverter is not IJsonArrayTupleCodecProvider<TRest> provider)
        {
            throw new ArgumentException(
                Resources.FormatTupleConverterMustBeCreatedByFactory(typeof(TRest), nameof(JsonArrayTupleConverters)),
                nameof(restConverter));
        }

        return provider.Codec;
    }

    private static Type[] CombineElementTypes(Type[] directElementTypes, IReadOnlyList<Type> restElementTypes)
    {
        var elementTypes = new Type[directElementTypes.Length + restElementTypes.Count];
        directElementTypes.CopyTo(elementTypes, 0);
        for (var i = 0; i < restElementTypes.Count; i++)
        {
            elementTypes[directElementTypes.Length + i] = restElementTypes[i];
        }

        return elementTypes;
    }

    private static TElement ReadElement<TElement>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (!reader.Read() || reader.TokenType == JsonTokenType.EndArray)
        {
            throw new JsonException(Resources.TupleJsonArrayHasFewerElementsThanContract);
        }

        return (TElement)JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(TElement)))!;
    }

    private static void WriteElement<TElement>(Utf8JsonWriter writer, TElement value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options.GetTypeInfo(typeof(TElement)));
}

internal delegate TTuple JsonArrayTupleReadElements<TTuple>(ref Utf8JsonReader reader, JsonSerializerOptions options);

internal delegate void JsonArrayTupleWriteElements<TTuple>(Utf8JsonWriter writer, TTuple value, JsonSerializerOptions options);

internal sealed class JsonArrayTupleCodec<TTuple>(
    IReadOnlyList<Type> elementTypes,
    JsonArrayTupleReadElements<TTuple> readElements,
    JsonArrayTupleWriteElements<TTuple> writeElements)
{
    public IReadOnlyList<Type> ElementTypes { get; } = elementTypes;

    public TTuple ReadElements(ref Utf8JsonReader reader, JsonSerializerOptions options)
        => readElements(ref reader, options);

    public void WriteElements(Utf8JsonWriter writer, TTuple value, JsonSerializerOptions options)
        => writeElements(writer, value, options);
}

internal interface IJsonArrayTupleCodecProvider<TTuple>
{
    JsonArrayTupleCodec<TTuple> Codec { get; }
}

internal sealed class ClosedJsonArrayTupleConverter<TTuple>(
    JsonArrayTupleContract contract,
    JsonArrayTupleCodec<TTuple> codec)
    : JsonConverter<TTuple>, IJsonArrayTupleConverter, IJsonArrayTupleCodecProvider<TTuple>, IOpenApiSchemaEvidenceProvider
{
    public JsonArrayTupleContract Contract { get; } = contract;

    public JsonArrayTupleCodec<TTuple> Codec { get; } = codec;

    OpenApiSchemaEvidence? IOpenApiSchemaEvidenceProvider.GetSchemaEvidence(OpenApiSchemaEvidenceContext context)
        => context.EffectiveType == typeof(TTuple)
            ? JsonArrayTupleSchemaEvidence.Create(Contract)
            : null;

    public override TTuple Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(Resources.FormatExpectedJsonArrayForTupleType(typeToConvert));
        }

        var value = Codec.ReadElements(ref reader, options);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException(Resources.FormatTupleJsonArrayHasMoreElements(typeToConvert, Contract.ElementTypes.Count));
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, TTuple value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        Codec.WriteElements(writer, value, options);
        writer.WriteEndArray();
    }
}
