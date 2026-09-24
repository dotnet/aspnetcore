// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Specifies how conventional scalar formats are selected for inferred OpenAPI schemas.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiScalarFormatPolicy
{
    /// <summary>
    /// Emits conventional well-known formats, including formats used as client-generation hints.
    /// </summary>
    Conventional = 0,

    /// <summary>
    /// Emits formats only when they are compatible with the complete proven runtime contract.
    /// </summary>
    CompatibleOnly = 1,

    /// <summary>
    /// Does not emit optional scalar formats.
    /// </summary>
    None = 2,
}

/// <summary>
/// Identifies where a scalar schema is used.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiScalarFormatLocation
{
    /// <summary>
    /// The scalar is a JSON request or response body.
    /// </summary>
    JsonBody = 0,

    /// <summary>
    /// The scalar is a property or nested value in a JSON request or response body.
    /// </summary>
    JsonProperty = 1,

    /// <summary>
    /// The scalar is bound from a route value.
    /// </summary>
    Route = 2,

    /// <summary>
    /// The scalar is bound from a query string.
    /// </summary>
    Query = 3,

    /// <summary>
    /// The scalar is bound from a request header.
    /// </summary>
    Header = 4,

    /// <summary>
    /// The scalar is bound from a form field.
    /// </summary>
    Form = 5,
}

/// <summary>
/// Identifies the serializer direction represented by a scalar schema.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiScalarFormatPurpose
{
    /// <summary>
    /// The schema is not associated with a specific serializer direction.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// The schema represents request input.
    /// </summary>
    Input = 1,

    /// <summary>
    /// The schema represents response output.
    /// </summary>
    Output = 2,
}

/// <summary>
/// Identifies the source of the effective scalar contract.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiScalarFormatProvenance
{
    /// <summary>
    /// The effective scalar contract is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The contract is provided by a built-in System.Text.Json converter.
    /// </summary>
    SystemTextJsonBuiltIn = 1,

    /// <summary>
    /// The contract is provided by a built-in ASP.NET Core parameter parser.
    /// </summary>
    FrameworkBuiltInParser = 2,

    /// <summary>
    /// The contract is provided by a custom System.Text.Json converter.
    /// </summary>
    CustomConverter = 3,

    /// <summary>
    /// The contract is provided by a custom parameter parser.
    /// </summary>
    CustomParser = 4,
}

/// <summary>
/// Provides information used to select a scalar format for an inferred OpenAPI schema.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiScalarFormatContext
{
    internal OpenApiScalarFormatContext(
        Type type,
        Type effectiveType,
        OpenApiScalarFormatLocation location,
        OpenApiScalarFormatPurpose purpose,
        OpenApiSpecVersion openApiVersion,
        OpenApiScalarFormatProvenance provenance,
        string? defaultFormat)
    {
        Type = type;
        EffectiveType = effectiveType;
        Location = location;
        Purpose = purpose;
        OpenApiVersion = openApiVersion;
        Provenance = provenance;
        DefaultFormat = defaultFormat;
    }

    /// <summary>
    /// Gets the declared CLR type associated with the scalar schema.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the effective non-nullable CLR type associated with the scalar contract.
    /// </summary>
    public Type EffectiveType { get; }

    /// <summary>
    /// Gets the location where the scalar schema is used.
    /// </summary>
    public OpenApiScalarFormatLocation Location { get; }

    /// <summary>
    /// Gets the serializer direction represented by the scalar schema.
    /// </summary>
    public OpenApiScalarFormatPurpose Purpose { get; }

    /// <summary>
    /// Gets the OpenAPI specification version targeted by the document.
    /// </summary>
    public OpenApiSpecVersion OpenApiVersion { get; }

    /// <summary>
    /// Gets the source of the effective scalar contract.
    /// </summary>
    public OpenApiScalarFormatProvenance Provenance { get; }

    /// <summary>
    /// Gets the format selected by the global scalar format policy, or <see langword="null"/> when no format was selected.
    /// </summary>
    public string? DefaultFormat { get; }
}
