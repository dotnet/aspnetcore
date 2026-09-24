// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal enum InferredScalarContractProvenance
{
    Unknown,
    SystemTextJsonBuiltIn,
}

internal enum InferredScalarContractKind
{
    Other,
    Integral,
    Base64String,
}

internal sealed record InferredNumericBoundsFact(string Minimum, string Maximum);

internal static class InferredNumericBoundsFactBuilder
{
    public static InferredNumericBoundsFact? Build(Type type, bool includeNativeIntegers = false)
    {
        return type switch
        {
            _ when type == typeof(sbyte) => Create(sbyte.MinValue, sbyte.MaxValue),
            _ when type == typeof(byte) => Create(byte.MinValue, byte.MaxValue),
            _ when type == typeof(short) => Create(short.MinValue, short.MaxValue),
            _ when type == typeof(ushort) => Create(ushort.MinValue, ushort.MaxValue),
            _ when type == typeof(int) => Create(int.MinValue, int.MaxValue),
            _ when type == typeof(uint) => Create(uint.MinValue, uint.MaxValue),
            _ when type == typeof(long) => Create(long.MinValue, long.MaxValue),
            _ when type == typeof(ulong) => Create(ulong.MinValue, ulong.MaxValue),
            _ when type == typeof(Int128) => Create(Int128.MinValue, Int128.MaxValue),
            _ when type == typeof(UInt128) => Create(UInt128.MinValue, UInt128.MaxValue),
            _ when includeNativeIntegers && type == typeof(nint) && IntPtr.Size == sizeof(long) => Create(long.MinValue, long.MaxValue),
            _ when includeNativeIntegers && type == typeof(nint) => Create(int.MinValue, int.MaxValue),
            _ when includeNativeIntegers && type == typeof(nuint) && UIntPtr.Size == sizeof(ulong) => Create(ulong.MinValue, ulong.MaxValue),
            _ when includeNativeIntegers && type == typeof(nuint) => Create(uint.MinValue, uint.MaxValue),
            _ => null,
        };

        static InferredNumericBoundsFact Create<T>(T minimum, T maximum)
            where T : IFormattable
            => new(
                minimum.ToString(format: null, CultureInfo.InvariantCulture),
                maximum.ToString(format: null, CultureInfo.InvariantCulture));
    }
}

internal sealed record InferredScalarContractFact(
    Type Type,
    InferredScalarContractProvenance Provenance,
    InferredScalarContractKind Kind,
    InferredNumericBoundsFact? NumericBounds);

internal sealed record InferredScalarSchemaDecision(
    string? Format,
    InferredNumericBoundsFact? NumericBounds,
    string? ContentEncoding);

internal static class InferredScalarContractFactBuilder
{
    public static InferredScalarContractFact Build(
        JsonTypeInfo typeInfo,
        JsonConverter? propertyConverter = null,
        bool hasConverterAttribute = false)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;
        var converter = propertyConverter ?? typeInfo.Converter;
        var provenance = !hasConverterAttribute &&
            converter.GetType().Assembly == typeof(JsonSerializerOptions).Assembly
            ? InferredScalarContractProvenance.SystemTextJsonBuiltIn
            : InferredScalarContractProvenance.Unknown;

        if (provenance == InferredScalarContractProvenance.Unknown)
        {
            return new(type, provenance, InferredScalarContractKind.Other, NumericBounds: null);
        }

        if (type == typeof(byte[]) || type == typeof(Memory<byte>) || type == typeof(ReadOnlyMemory<byte>))
        {
            return new(type, provenance, InferredScalarContractKind.Base64String, NumericBounds: null);
        }

        var numericBounds = InferredNumericBoundsFactBuilder.Build(type);
        return new(
            type,
            provenance,
            numericBounds is null ? InferredScalarContractKind.Other : InferredScalarContractKind.Integral,
            numericBounds);
    }
}

internal static class InferredScalarSchemaDecisionBuilder
{
    private static readonly IReadOnlyDictionary<Type, string> _legacyFormats = new Dictionary<Type, string>
    {
        [typeof(byte)] = "uint8",
        [typeof(byte[])] = "byte",
        [typeof(int)] = "int32",
        [typeof(uint)] = "uint32",
        [typeof(long)] = "int64",
        [typeof(ulong)] = "uint64",
        [typeof(short)] = "int16",
        [typeof(ushort)] = "uint16",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "double",
        [typeof(DateTime)] = "date-time",
        [typeof(DateTimeOffset)] = "date-time",
        [typeof(Guid)] = "uuid",
        [typeof(char)] = "char",
        [typeof(Uri)] = "uri",
        [typeof(TimeOnly)] = "time",
        [typeof(DateOnly)] = "date",
    };

    public static InferredScalarSchemaDecision Build(InferredScalarContractFact fact)
    {
        if (fact.Provenance == InferredScalarContractProvenance.Unknown)
        {
            return new(Format: null, NumericBounds: null, ContentEncoding: null);
        }

        if (fact.Kind == InferredScalarContractKind.Base64String)
        {
            return new(Format: null, NumericBounds: null, ContentEncoding: "base64");
        }

        var format = fact.Type == typeof(decimal) || fact.Type == typeof(byte[])
            ? null
            : GetLegacyFormat(fact.Type);

        return new(format, fact.NumericBounds, ContentEncoding: null);
    }

    internal static string? GetLegacyFormat(Type type)
        => _legacyFormats.GetValueOrDefault(Nullable.GetUnderlyingType(type) ?? type);
}
