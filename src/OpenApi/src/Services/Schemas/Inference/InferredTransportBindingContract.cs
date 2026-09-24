// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.OpenApi;

internal enum InferredTransportBindingSource
{
    Header,
    Query,
    Path,
    Form,
}

internal enum InferredTransportBindingProvenance
{
    Unknown,
    FrameworkBuiltIn,
    EnumTryParse,
    CustomParser,
}

internal enum InferredTransportSchemaKind
{
    Unknown,
    String,
    Boolean,
    Integer,
    Number,
    Enum,
    Array,
}

internal sealed record InferredTransportBindingFact(
    Type Type,
    InferredTransportBindingSource Source,
    InferredTransportBindingProvenance Provenance,
    InferredTransportSchemaKind Kind,
    InferredNumericBoundsFact? NumericBounds,
    InferredTransportBindingFact? Element)
{
    public bool IsOptional { get; init; }

    public bool HasDefaultValue { get; init; }

    public object? DefaultValue { get; init; }
}

internal sealed record InferredTransportSchemaDecision(
    InferredTransportSchemaKind Kind,
    InferredNumericBoundsFact? NumericBounds,
    InferredTransportSchemaDecision? Items)
{
    public bool IsKnown => Kind != InferredTransportSchemaKind.Unknown;
}

internal static class InferredTransportBindingFactBuilder
{
    public static InferredTransportBindingFact Build(
        Type type,
        BindingSource source,
        IParameterBindingMetadata? bindingMetadata)
    {
        var transportSource = source == BindingSource.Header
            ? InferredTransportBindingSource.Header
            : source == BindingSource.Path
                ? InferredTransportBindingSource.Path
                : source == BindingSource.Form
                    ? InferredTransportBindingSource.Form
                    : InferredTransportBindingSource.Query;
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType.IsArray)
        {
            var element = BuildScalar(
                effectiveType.GetElementType()!,
                transportSource,
                bindingMetadata?.HasTryParse == true);
            var arrayFact = element.Kind == InferredTransportSchemaKind.Unknown
                ? Unknown(effectiveType, transportSource)
                : new(
                    effectiveType,
                    transportSource,
                    element.Provenance,
                    InferredTransportSchemaKind.Array,
                    NumericBounds: null,
                    element);
            return WithParameterFacts(arrayFact, bindingMetadata);
        }

        return WithParameterFacts(BuildScalar(
            effectiveType,
            transportSource,
            bindingMetadata?.HasTryParse == true
                || effectiveType == typeof(string)
                || transportSource != InferredTransportBindingSource.Form), bindingMetadata);
    }

    private static InferredTransportBindingFact BuildScalar(
        Type type,
        InferredTransportBindingSource source,
        bool hasTryParse)
    {
        if (type.IsEnum)
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.EnumTryParse,
                InferredTransportSchemaKind.Enum,
                InferredNumericBoundsFactBuilder.Build(Enum.GetUnderlyingType(type)),
                Element: null);
        }

        if (type == typeof(string) || IsFrameworkStringParser(type))
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.FrameworkBuiltIn,
                InferredTransportSchemaKind.String,
                NumericBounds: null,
                Element: null);
        }

        if (type == typeof(bool))
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.FrameworkBuiltIn,
                InferredTransportSchemaKind.Boolean,
                NumericBounds: null,
                Element: null);
        }

        if (IsIntegral(type))
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.FrameworkBuiltIn,
                InferredTransportSchemaKind.Integer,
                InferredNumericBoundsFactBuilder.Build(type, includeNativeIntegers: true),
                Element: null);
        }

        if (type == typeof(BigInteger))
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.FrameworkBuiltIn,
                InferredTransportSchemaKind.Integer,
                NumericBounds: null,
                Element: null);
        }

        if (type == typeof(Half) || type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return new(
                type,
                source,
                InferredTransportBindingProvenance.FrameworkBuiltIn,
                InferredTransportSchemaKind.Number,
                NumericBounds: null,
                Element: null);
        }

        if (!hasTryParse)
        {
            return Unknown(type, source);
        }

        return new(
            type,
            source,
            InferredTransportBindingProvenance.CustomParser,
            InferredTransportSchemaKind.String,
            NumericBounds: null,
            Element: null);
    }

    private static bool IsIntegral(Type type)
        => type == typeof(sbyte)
            || type == typeof(byte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(Int128)
            || type == typeof(UInt128)
            || type == typeof(nint)
            || type == typeof(nuint);

    private static bool IsFrameworkStringParser(Type type)
        => type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(Uri)
            || type == typeof(Version)
            || type == typeof(char)
            || type == typeof(System.Text.Rune)
            || type == typeof(System.Net.IPAddress)
            || type == typeof(System.Net.IPEndPoint);

    private static InferredTransportBindingFact Unknown(Type type, InferredTransportBindingSource source)
        => new(
            type,
            source,
            InferredTransportBindingProvenance.Unknown,
            InferredTransportSchemaKind.Unknown,
            NumericBounds: null,
            Element: null);

    private static InferredTransportBindingFact WithParameterFacts(
        InferredTransportBindingFact fact,
        IParameterBindingMetadata? bindingMetadata)
        => bindingMetadata is null
            ? fact
            : fact with
            {
                IsOptional = bindingMetadata.IsOptional,
                HasDefaultValue = bindingMetadata.ParameterInfo.HasDefaultValue,
                DefaultValue = bindingMetadata.ParameterInfo.HasDefaultValue
                    ? bindingMetadata.ParameterInfo.DefaultValue
                    : null,
            };
}

internal static class InferredTransportSchemaDecisionBuilder
{
    public static InferredTransportSchemaDecision Build(InferredTransportBindingFact fact)
        => new(
            fact.Kind,
            fact.NumericBounds,
            fact.Element is null ? null : Build(fact.Element));
}
