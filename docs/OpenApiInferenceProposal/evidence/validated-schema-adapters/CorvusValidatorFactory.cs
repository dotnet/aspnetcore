// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040

using System.Collections.Concurrent;
using Corvus.Text.Json;
using Corvus.Text.Json.Validator;
using Microsoft.AspNetCore.OpenApi;

namespace ValidatedSchemaAdapters;

internal sealed class CorvusValidatorFactory : IOpenApiJsonSchemaValidatorFactory
{
    private readonly ConcurrentDictionary<ValidatorCacheKey, IOpenApiJsonSchemaValidator> _validators = new();

    public IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence)
        => _validators.GetOrAdd(
            new(evidence.Identity, evidence.Dialect, evidence.ValidationCapabilities),
            _ => Compile(evidence));

    private static IOpenApiJsonSchemaValidator Compile(OpenApiValidatedJsonSchemaEvidence evidence)
    {
        var options = new JsonSchema.Options(
            alwaysAssertFormat: evidence.ValidationCapabilities.HasFlag(
                OpenApiJsonSchemaValidationCapabilities.FormatAssertions));
        return new Validator(JsonSchema.FromText(
            evidence.Schema.GetRawText(),
            canonicalUri: $"urn:sha256:{evidence.Identity}",
            options: options));
    }

    private readonly record struct ValidatorCacheKey(
        string Identity,
        OpenApiJsonSchemaDialect Dialect,
        OpenApiJsonSchemaValidationCapabilities Capabilities);

    private sealed class Validator(JsonSchema schema) : IOpenApiJsonSchemaValidator
    {
        public ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
            ReadOnlyMemory<byte> utf8Json,
            OpenApiJsonSchemaValidationContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (schema.Validate(utf8Json))
            {
                return ValueTask.FromResult(OpenApiJsonSchemaValidationResult.Valid);
            }

            return ValueTask.FromResult(new OpenApiJsonSchemaValidationResult(
            [
                new(string.Empty, "schema", "The JSON payload does not satisfy the validated schema."),
            ]));
        }
    }
}
