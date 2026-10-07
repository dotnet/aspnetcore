// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal static class OpenApiScalarFormatResolver
{
    public static string? ResolveJsonFormat(
        OpenApiOptions options,
        InferredScalarContractFact fact,
        Type declaredType,
        OpenApiScalarFormatLocation location,
        InferredSchemaPurpose purpose,
        OpenApiSpecVersion openApiVersion)
    {
        var defaultFormat = fact.Provenance switch
        {
            InferredScalarContractProvenance.SystemTextJsonBuiltIn => GetPolicyFormat(options.ScalarFormatPolicy, fact.Type, isTransport: false),
            InferredScalarContractProvenance.RecognizedProvider => GetProviderFormat(
                options.ScalarFormatPolicy,
                fact.Type,
                fact.RecognizedFormat),
            _ => null,
        };
        var context = new OpenApiScalarFormatContext(
            declaredType,
            fact.Type,
            location,
            MapPurpose(purpose),
            openApiVersion,
            fact.Provenance switch
            {
                InferredScalarContractProvenance.SystemTextJsonBuiltIn => OpenApiScalarFormatProvenance.SystemTextJsonBuiltIn,
                InferredScalarContractProvenance.CustomConverter or InferredScalarContractProvenance.RecognizedProvider => OpenApiScalarFormatProvenance.CustomConverter,
                _ => OpenApiScalarFormatProvenance.Unknown,
            },
            defaultFormat);
        return options.CreateScalarFormat is { } callback
            ? callback(context)
            : defaultFormat;
    }

    public static string? ResolveTransportFormat(
        OpenApiOptions options,
        InferredTransportBindingFact fact,
        OpenApiSpecVersion openApiVersion)
    {
        var defaultFormat = fact.Provenance == InferredTransportBindingProvenance.FrameworkBuiltIn
            ? GetPolicyFormat(options.ScalarFormatPolicy, fact.Type, isTransport: true)
            : null;
        var context = new OpenApiScalarFormatContext(
            fact.DeclaredType,
            fact.Type,
            fact.Source switch
            {
                InferredTransportBindingSource.Header => OpenApiScalarFormatLocation.Header,
                InferredTransportBindingSource.Query => OpenApiScalarFormatLocation.Query,
                InferredTransportBindingSource.Path => OpenApiScalarFormatLocation.Route,
                InferredTransportBindingSource.Form => OpenApiScalarFormatLocation.Form,
                _ => throw new InvalidOperationException(),
            },
            OpenApiScalarFormatPurpose.Input,
            openApiVersion,
            fact.Provenance switch
            {
                InferredTransportBindingProvenance.FrameworkBuiltIn => OpenApiScalarFormatProvenance.FrameworkBuiltInParser,
                InferredTransportBindingProvenance.CustomParser => OpenApiScalarFormatProvenance.CustomParser,
                _ => OpenApiScalarFormatProvenance.Unknown,
            },
            defaultFormat);
        return options.CreateScalarFormat is { } callback
            ? callback(context)
            : defaultFormat;
    }

    private static string? GetPolicyFormat(
        OpenApiScalarFormatPolicy policy,
        Type type,
        bool isTransport)
    {
        if (policy == OpenApiScalarFormatPolicy.None)
        {
            return null;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        var compatibleFormat = type switch
        {
            _ when !isTransport && type == typeof(Guid) => "uuid",
            _ when !isTransport && type == typeof(DateOnly) => "date",
            _ when type == typeof(byte) => "uint8",
            _ when type == typeof(short) => "int16",
            _ when type == typeof(ushort) => "uint16",
            _ when type == typeof(int) => "int32",
            _ when type == typeof(uint) => "uint32",
            _ when type == typeof(long) => "int64",
            _ when type == typeof(ulong) => "uint64",
            _ => null,
        };
        if (compatibleFormat is not null || policy == OpenApiScalarFormatPolicy.CompatibleOnly)
        {
            return compatibleFormat;
        }

        return type switch
        {
            _ when type == typeof(Guid) => "uuid",
            _ when type == typeof(Uri) => "uri-reference",
            _ when type == typeof(DateOnly) => "date",
            _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => "date-time",
            _ when type == typeof(TimeOnly) => "time",
            _ when type == typeof(float) => "float",
            _ when type == typeof(double) => "double",
            _ when type == typeof(char) => "char",
            _ => null,
        };
    }

    private static string? GetProviderFormat(
        OpenApiScalarFormatPolicy policy,
        Type type,
        string? format)
        => policy switch
        {
            OpenApiScalarFormatPolicy.None => null,
            OpenApiScalarFormatPolicy.CompatibleOnly
                when format != GetPolicyFormat(policy, type, isTransport: false) => null,
            _ => format,
        };

    private static OpenApiScalarFormatPurpose MapPurpose(InferredSchemaPurpose purpose)
        => purpose switch
        {
            InferredSchemaPurpose.Input => OpenApiScalarFormatPurpose.Input,
            InferredSchemaPurpose.Output => OpenApiScalarFormatPurpose.Output,
            _ => OpenApiScalarFormatPurpose.Neutral,
        };
}
