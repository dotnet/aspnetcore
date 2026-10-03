// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Describes JSON Schema data generated ahead of time.
/// </summary>
/// <typeparam name="TSelf">The generated artifact type.</typeparam>
/// <remarks>
/// Implementations are normally source generated. All returned data must be immutable and independent
/// of the path from which the source schema was read.
/// </remarks>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiValidatedJsonSchemaArtifact<TSelf>
    where TSelf : IOpenApiValidatedJsonSchemaArtifact<TSelf>
{
    /// <summary>
    /// Gets the exact source JSON Schema UTF-8 bytes.
    /// </summary>
    static abstract ReadOnlyMemory<byte> SourceSchema { get; }

    /// <summary>
    /// Gets the canonical normalized JSON Schema UTF-8 bytes.
    /// </summary>
    static abstract ReadOnlyMemory<byte> NormalizedSchema { get; }

    /// <summary>
    /// Gets a canonical JSON array containing the resolved local JSON Pointer references.
    /// </summary>
    static abstract ReadOnlyMemory<byte> LocalReferences { get; }

    /// <summary>
    /// Gets the dialect in which <see cref="SourceSchema"/> is interpreted.
    /// </summary>
    static abstract OpenApiJsonSchemaDialect Dialect { get; }

    /// <summary>
    /// Gets the validation capabilities that affect schema semantics.
    /// </summary>
    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the lowercase hexadecimal SHA-256 identity of the exact source schema bytes.
    /// </summary>
    static abstract string SchemaIdentity { get; }

    /// <summary>
    /// Creates an isolated mutable OpenAPI schema for the requested target specification.
    /// </summary>
    /// <param name="openApiVersion">The target OpenAPI specification version.</param>
    /// <returns>A new schema instance that is not shared with another call.</returns>
    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);
}

/// <summary>
/// Validates raw UTF-8 JSON using code generated or compiled ahead of time for a schema artifact.
/// </summary>
/// <typeparam name="TArtifact">The schema artifact type.</typeparam>
/// <typeparam name="TSelf">The generated validator type.</typeparam>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
    where TArtifact : IOpenApiValidatedJsonSchemaArtifact<TArtifact>
    where TSelf : IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
{
    /// <summary>
    /// Gets a stable identity for validator options that affect validation semantics.
    /// </summary>
    static abstract string ConfigurationIdentity { get; }

    /// <summary>
    /// Validates a complete UTF-8 JSON payload.
    /// </summary>
    /// <param name="utf8Json">The complete UTF-8 JSON payload.</param>
    /// <param name="purpose">The serializer direction being validated.</param>
    /// <param name="cancellationToken">A token that can cancel validation.</param>
    /// <returns>The validation result.</returns>
    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Binds a generated JSON Schema artifact to a generated or ahead-of-time compiled validator.
/// </summary>
/// <typeparam name="TSelf">The generated binding type.</typeparam>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiValidatedJsonSchemaBinding<TSelf>
    where TSelf : IOpenApiValidatedJsonSchemaBinding<TSelf>
{
    /// <summary>
    /// Gets the CLR type whose JSON representation is described by the binding.
    /// </summary>
    static abstract Type Type { get; }

    /// <summary>
    /// Gets the exact source-schema identity.
    /// </summary>
    static abstract string SchemaIdentity { get; }

    /// <summary>
    /// Gets the composite schema, capability, and validator-configuration identity.
    /// </summary>
    static abstract string Identity { get; }

    /// <summary>
    /// Gets the source JSON Schema dialect.
    /// </summary>
    static abstract OpenApiJsonSchemaDialect Dialect { get; }

    /// <summary>
    /// Gets the validation capabilities that affect schema semantics.
    /// </summary>
    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }

    /// <summary>
    /// Creates an isolated mutable OpenAPI schema for the requested target specification.
    /// </summary>
    /// <param name="openApiVersion">The target OpenAPI specification version.</param>
    /// <returns>A new schema instance that is not shared with another call.</returns>
    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);

    /// <summary>
    /// Validates a complete UTF-8 JSON payload.
    /// </summary>
    /// <param name="utf8Json">The complete UTF-8 JSON payload.</param>
    /// <param name="purpose">The serializer direction being validated.</param>
    /// <param name="cancellationToken">A token that can cancel validation.</param>
    /// <returns>The validation result.</returns>
    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default);
}
