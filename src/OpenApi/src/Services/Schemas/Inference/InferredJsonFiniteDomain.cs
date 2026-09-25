// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal enum InferredJsonLiteralKind
{
    Null,
    Boolean,
    Number,
    String,
}

internal readonly record struct InferredJsonLiteral(InferredJsonLiteralKind Kind, string CanonicalValue)
{
    public bool IsInteger
        => Kind == InferredJsonLiteralKind.Number &&
            (CanonicalValue == "0" ||
            BigInteger.Parse(CanonicalValue.AsSpan(CanonicalValue.IndexOf('e') + 1), CultureInfo.InvariantCulture) >= 0);

    public static InferredJsonLiteral Create(JsonNode? value)
    {
        if (value is null)
        {
            return new(InferredJsonLiteralKind.Null, "null");
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.False => new(InferredJsonLiteralKind.Boolean, "false"),
            JsonValueKind.True => new(InferredJsonLiteralKind.Boolean, "true"),
            JsonValueKind.Number => new(InferredJsonLiteralKind.Number, CanonicalizeNumber(value.ToJsonString())),
            JsonValueKind.String => new(InferredJsonLiteralKind.String, value.GetValue<string>()),
            _ => throw new InvalidOperationException(Resources.FormatUnsupportedFiniteDomainLiteral(value)),
        };
    }

    private static string CanonicalizeNumber(string value)
    {
        var span = value.AsSpan();
        var isNegative = span[0] == '-';
        if (isNegative)
        {
            span = span[1..];
        }

        var exponentIndex = span.IndexOfAny('e', 'E');
        var significand = exponentIndex < 0 ? span : span[..exponentIndex];
        var exponent = exponentIndex < 0
            ? BigInteger.Zero
            : BigInteger.Parse(span[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var decimalIndex = significand.IndexOf('.');
        var integerDigits = decimalIndex < 0 ? significand.Length : decimalIndex;
        var digits = decimalIndex < 0
            ? significand.ToString()
            : string.Concat(significand[..integerDigits], significand[(decimalIndex + 1)..]);
        var firstNonZero = 0;
        while (firstNonZero < digits.Length && digits[firstNonZero] == '0')
        {
            firstNonZero++;
        }

        if (firstNonZero == digits.Length)
        {
            return "0";
        }

        digits = digits[firstNonZero..];
        exponent += integerDigits - firstNonZero - digits.Length;
        var lastNonZero = digits.Length - 1;
        while (digits[lastNonZero] == '0')
        {
            lastNonZero--;
            exponent++;
        }

        digits = digits[..(lastNonZero + 1)];
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(isNegative ? "-" : string.Empty)}{digits}e{exponent}");
    }
}

internal enum InferredJsonFiniteDomainReason
{
    SchemaEnum,
    SchemaConst,
    UnionOfFiniteDomains,
    CustomConverter,
    FlagsEnum,
    IntegerValuesAccepted,
    NoFiniteSchema,
    UnsupportedSchema,
}

internal sealed record InferredJsonFiniteDomainFact(
    bool IsExact,
    InferredJsonFiniteDomainReason Reason,
    IReadOnlyList<InferredJsonLiteral> Values);

internal static class InferredJsonFiniteDomainBuilder
{
    public static InferredJsonFiniteDomainFact? Build(
        JsonTypeInfo typeInfo,
        bool hasCustomConverter)
    {
        if (!typeInfo.Type.IsEnum)
        {
            return null;
        }

        if (hasCustomConverter)
        {
            return Open(InferredJsonFiniteDomainReason.CustomConverter);
        }

        if (typeInfo.Type.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            return Open(InferredJsonFiniteDomainReason.FlagsEnum);
        }

        if (SerializerAcceptsInteger(typeInfo))
        {
            return Open(InferredJsonFiniteDomainReason.IntegerValuesAccepted);
        }

        var schema = typeInfo.GetJsonSchemaAsNode(
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });
        return ExtractSchema(schema);
    }

    internal static InferredJsonFiniteDomainFact ExtractSchema(JsonNode schema)
    {
        if (schema is not JsonObject schemaObject)
        {
            return Open(InferredJsonFiniteDomainReason.UnsupportedSchema);
        }

        var hasEnum = schemaObject.TryGetPropertyValue(OpenApiSchemaKeywords.EnumKeyword, out var enumNode);
        var hasConstant = schemaObject.TryGetPropertyValue(OpenApiSchemaKeywords.ConstKeyword, out var constant);
        if (hasEnum && hasConstant)
        {
            return Open(InferredJsonFiniteDomainReason.UnsupportedSchema);
        }

        if (hasEnum)
        {
            return enumNode is JsonArray enumValues
                ? CreateExact(enumValues, InferredJsonFiniteDomainReason.SchemaEnum)
                : Open(InferredJsonFiniteDomainReason.UnsupportedSchema);
        }

        if (hasConstant)
        {
            return CreateExact([constant], InferredJsonFiniteDomainReason.SchemaConst);
        }

        return Open(InferredJsonFiniteDomainReason.NoFiniteSchema);
    }

    private static bool SerializerAcceptsInteger(JsonTypeInfo typeInfo)
    {
        // EnumConverter.GetSchema intentionally omits its integer alternative when strings are enabled.
        // Probe the same public serializer contract so that such schemas are never treated as closed.
        try
        {
            _ = JsonSerializer.Deserialize("0", typeInfo);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static InferredJsonFiniteDomainFact CreateExact(
        IEnumerable<JsonNode?> values,
        InferredJsonFiniteDomainReason reason)
    {
        try
        {
            var literals = values
                .Select(InferredJsonLiteral.Create)
                .Distinct()
                .OrderBy(literal => literal.Kind)
                .ThenBy(literal => literal.CanonicalValue, StringComparer.Ordinal)
                .ToArray();
            return literals.Length == 0
                ? Open(InferredJsonFiniteDomainReason.UnsupportedSchema)
                : new(true, reason, new ReadOnlyCollection<InferredJsonLiteral>(literals));
        }
        catch (InvalidOperationException)
        {
            return Open(InferredJsonFiniteDomainReason.UnsupportedSchema);
        }
    }

    private static InferredJsonFiniteDomainFact Open(InferredJsonFiniteDomainReason reason)
        => new(false, reason, Array.Empty<InferredJsonLiteral>());
}
