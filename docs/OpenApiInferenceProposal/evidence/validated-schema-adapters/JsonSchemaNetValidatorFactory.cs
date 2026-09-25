// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040

using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;
using Microsoft.AspNetCore.OpenApi;

namespace ValidatedSchemaAdapters;

internal sealed class JsonSchemaNetValidatorFactory : IOpenApiJsonSchemaValidatorFactory
{
    private readonly ConcurrentDictionary<ValidatorCacheKey, IOpenApiJsonSchemaValidator> _validators = new();

    public IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence)
        => _validators.GetOrAdd(
            new(evidence.Identity, evidence.Dialect, evidence.ValidationCapabilities),
            _ => Compile(evidence));

    private static IOpenApiJsonSchemaValidator Compile(OpenApiValidatedJsonSchemaEvidence evidence)
    {
        var buildOptions = new BuildOptions
        {
            Dialect = Dialect.Draft202012,
            SchemaRegistry = new SchemaRegistry(),
        };
        var schema = JsonSchema.FromText(evidence.Schema.GetRawText(), buildOptions);
        var evaluationOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = evidence.ValidationCapabilities.HasFlag(
                OpenApiJsonSchemaValidationCapabilities.FormatAssertions),
        };
        return new Validator(schema, evaluationOptions);
    }

    private readonly record struct ValidatorCacheKey(
        string Identity,
        OpenApiJsonSchemaDialect Dialect,
        OpenApiJsonSchemaValidationCapabilities Capabilities);

    private sealed class Validator(JsonSchema schema, EvaluationOptions options) : IOpenApiJsonSchemaValidator
    {
        public ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
            ReadOnlyMemory<byte> utf8Json,
            OpenApiJsonSchemaValidationContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(utf8Json);
            var result = schema.Evaluate(document.RootElement, options);
            return ValueTask.FromResult(result.IsValid
                ? OpenApiJsonSchemaValidationResult.Valid
                : new OpenApiJsonSchemaValidationResult(
                [
                    new(string.Empty, "schema", "The JSON payload does not satisfy the validated schema."),
                ]));
        }
    }
}
