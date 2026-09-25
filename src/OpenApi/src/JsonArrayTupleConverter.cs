// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Converts <see cref="Tuple"/> and <see cref="ValueTuple"/> values to and from positional JSON arrays.
/// </summary>
/// <remarks>
/// This converter dynamically constructs closed generic converters and is not supported in
/// NativeAOT or trimming-sensitive applications.
/// </remarks>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
[RequiresDynamicCode("Tuple converters are constructed for tuple types discovered at run time.")]
[RequiresUnreferencedCode("Tuple constructors and members are accessed for tuple types discovered at run time.")]
public sealed class JsonArrayTupleConverter : JsonConverterFactory, IOpenApiSchemaEvidenceProvider
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonArrayTupleConverter"/>.
    /// </summary>
    [RequiresDynamicCode("Tuple converters are constructed for tuple types discovered at run time.")]
    [RequiresUnreferencedCode("Tuple constructors and members are accessed for tuple types discovered at run time.")]
    public JsonArrayTupleConverter()
    {
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => JsonArrayTupleContract.TryCreate(typeToConvert, out _);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonArrayTupleContract.TryCreate(typeToConvert, out var contract))
        {
            throw new InvalidOperationException(Resources.FormatTypeNotSupportedTupleType(typeToConvert));
        }

        return CreateConverterCore(typeToConvert, contract);
    }

    OpenApiSchemaEvidence? IOpenApiSchemaEvidenceProvider.GetSchemaEvidence(OpenApiSchemaEvidenceContext context)
        => JsonArrayTupleSchemaEvidence.Create(context.EffectiveType);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The public factory and constructor communicate the approved dynamic-code requirement.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The public factory and constructor communicate the approved trimming requirement.")]
    private static JsonConverter CreateConverterCore(Type typeToConvert, JsonArrayTupleContract contract)
    {
        var converterType = typeof(JsonArrayTupleConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, contract)!;
    }
}

internal interface IJsonArrayTupleConverter
{
    JsonArrayTupleContract Contract { get; }
}

internal static class JsonArrayTupleSchemaEvidence
{
    public static OpenApiSchemaEvidence? Create(Type type)
        => JsonArrayTupleContract.TryCreate(type, out var contract) ? Create(contract) : null;

    public static OpenApiPositionalArraySchemaEvidence Create(JsonArrayTupleContract contract)
    {
        return new(contract.ElementTypes);
    }
}

internal sealed class JsonArrayTupleConverter<TTuple>(JsonArrayTupleContract contract)
    : JsonConverter<TTuple>, IJsonArrayTupleConverter, IOpenApiSchemaEvidenceProvider
{
    public JsonArrayTupleContract Contract { get; } = contract;

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

        var values = new object?[Contract.ElementTypes.Count];
        for (var i = 0; i < values.Length; i++)
        {
            if (!reader.Read() || reader.TokenType == JsonTokenType.EndArray)
            {
                throw new JsonException(Resources.FormatTupleJsonArrayHasFewerElements(typeToConvert, values.Length));
            }

            values[i] = JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(Contract.ElementTypes[i]));
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException(Resources.FormatTupleJsonArrayHasMoreElements(typeToConvert, values.Length));
        }

        return (TTuple)CreateTuple(values);
    }

    public override void Write(Utf8JsonWriter writer, TTuple value, JsonSerializerOptions options)
    {
        if (value is not ITuple tuple || tuple.Length != Contract.ElementTypes.Count)
        {
            throw new JsonException(Resources.FormatTupleValueDoesNotMatchContract(typeof(TTuple)));
        }

        writer.WriteStartArray();
        for (var i = 0; i < tuple.Length; i++)
        {
            JsonSerializer.Serialize(writer, tuple[i], options.GetTypeInfo(Contract.ElementTypes[i]));
        }
        writer.WriteEndArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The public factory and constructor communicate the approved trimming requirement.")]
    private object CreateTuple(IReadOnlyList<object?> values)
        => Contract.Create(values);
}

internal sealed class JsonArrayTupleContract
{
    private static readonly HashSet<Type> TupleDefinitions =
    [
        typeof(Tuple<>),
        typeof(Tuple<,>),
        typeof(Tuple<,,>),
        typeof(Tuple<,,,>),
        typeof(Tuple<,,,,>),
        typeof(Tuple<,,,,,>),
        typeof(Tuple<,,,,,,>),
        typeof(Tuple<,,,,,,,>),
        typeof(ValueTuple<>),
        typeof(ValueTuple<,>),
        typeof(ValueTuple<,,>),
        typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>),
        typeof(ValueTuple<,,,,,>),
        typeof(ValueTuple<,,,,,,>),
        typeof(ValueTuple<,,,,,,,>),
    ];

    private JsonArrayTupleContract(Type tupleType, IReadOnlyList<Type> elementTypes)
    {
        TupleType = tupleType;
        ElementTypes = elementTypes;
    }

    public Type TupleType { get; }

    public IReadOnlyList<Type> ElementTypes { get; }

    public static JsonArrayTupleContract Create(Type tupleType, IReadOnlyList<Type> elementTypes)
        => new(tupleType, elementTypes);

    public static bool TryCreate(Type type, [NotNullWhen(true)] out JsonArrayTupleContract? contract)
    {
        var elementTypes = new List<Type>();
        if (!TryFlatten(type, elementTypes))
        {
            contract = null;
            return false;
        }

        contract = new(type, elementTypes.AsReadOnly());
        return true;
    }

    [RequiresUnreferencedCode("Tuple constructors are accessed for tuple types discovered at run time.")]
    public object Create(IReadOnlyList<object?> values)
    {
        var index = 0;
        var result = CreateTuple(TupleType, values, ref index);
        if (index != values.Count)
        {
            throw new JsonException(Resources.FormatTupleContractDidNotConsumeAllValues(TupleType));
        }

        return result;
    }

    private static bool TryFlatten(Type type, List<Type> elementTypes)
    {
        if (type == typeof(ValueTuple))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        if (!TupleDefinitions.Contains(definition))
        {
            return false;
        }

        var arguments = type.GetGenericArguments();
        if (arguments.Length == 8)
        {
            elementTypes.AddRange(arguments.AsSpan(0, 7).ToArray());
            return TryFlatten(arguments[7], elementTypes);
        }

        if (arguments.Length is < 1 or > 7)
        {
            return false;
        }

        elementTypes.AddRange(arguments);
        return true;
    }

    [RequiresUnreferencedCode("Tuple constructors are accessed for tuple types discovered at run time.")]
    private static object CreateTuple(Type tupleType, IReadOnlyList<object?> values, ref int index)
    {
        if (tupleType == typeof(ValueTuple))
        {
            return default(ValueTuple);
        }

        var arguments = tupleType.GetGenericArguments();
        var constructorArguments = new object?[arguments.Length];
        var directElements = arguments.Length == 8 ? 7 : arguments.Length;
        for (var i = 0; i < directElements; i++)
        {
            constructorArguments[i] = values[index++];
        }

        if (arguments.Length == 8)
        {
            constructorArguments[7] = CreateTuple(arguments[7], values, ref index);
        }

        return Activator.CreateInstance(tupleType, constructorArguments)
            ?? throw new JsonException(Resources.FormatUnableToConstructTupleType(tupleType));
    }
}
