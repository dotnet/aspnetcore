// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Corvus.Text.Json.RuntimeEvaluator;
using GeneratedSchemaInspection;
using Json.Schema;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OpenApi.Generated;
using Microsoft.OpenApi;

namespace AnnotatedSchemaDemo;

internal static class DemoCorpus
{
    internal static readonly byte[][] Valid =
    [
        """{"kind":"business","address":{"street":"1 High Street","city":"London"},"taxId":"GB123"}"""u8.ToArray(),
        """{"kind":"personal","address":{"street":"1 High Street","city":"London"}}"""u8.ToArray(),
    ];

    internal static readonly byte[][] Invalid =
    [
        """{"kind":"business","address":{"street":"1 High Street","city":"London"}}"""u8.ToArray(),
        """{"kind":"personal"}"""u8.ToArray(),
        """{"kind":"personal","address":{"street":"1 High Street"}}"""u8.ToArray(),
        """{"kind":"personal","address":"not-an-object"}"""u8.ToArray(),
        """{"kind":1,"address":{"street":"1 High Street","city":"London"}}"""u8.ToArray(),
        """{"kind":"personal","address":{"street":"1 High Street","city":"London"}"""u8.ToArray(),
    ];
}

internal static class DemoValidation
{
    internal static OpenApiJsonSchemaValidationResult ToResult(bool valid)
        => valid
            ? OpenApiJsonSchemaValidationResult.Valid
            : new OpenApiJsonSchemaValidationResult(
            [
                new("/", "schema", "The payload does not satisfy the generated schema."),
            ]);
}

internal static class DemoBindingAdapter
{
    internal static string CreateIdentity(params ReadOnlySpan<string> validatorIdentity)
    {
        var graphIdentity = FlagshipAnnotatedArtifact.SchemaIdentity.ToUpperInvariant();
        var value = new StringBuilder(graphIdentity);
        foreach (var component in validatorIdentity)
        {
            value.Append('\n').Append(component);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString())));
    }

    internal static OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion, string bindingIdentity)
    {
        var schema = FlagshipAnnotatedArtifact.CreateOpenApiSchema(openApiVersion);
        schema.Metadata ??= new Dictionary<string, object>();
        schema.Metadata["x-schema-validated-identity"] = bindingIdentity;
        return schema;
    }
}

internal sealed class JsonSchemaNetNativeValidator :
    IOpenApiValidatedJsonSchemaValidator<FlagshipAnnotatedArtifact, JsonSchemaNetNativeValidator>
{
    private const string PackageVersion = "9.4.0";
    private const string OptionsIdentity = "draft2020-12;format=false;output=flag";
    private static readonly JsonSchema s_schema = InitializeNativeSchema();
    private static readonly EvaluationOptions s_options = new()
    {
        OutputFormat = OutputFormat.Flag,
        RequireFormatValidation = false,
    };
    private static int s_initializationCount;

    public static string ConfigurationIdentity => $"jsonschema-net-{PackageVersion};{OptionsIdentity}";
    internal static int InitializationCount => Volatile.Read(ref s_initializationCount);

    public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var document = JsonDocument.Parse(utf8Json);
            return ValueTask.FromResult(DemoValidation.ToResult(
                s_schema.Evaluate(document.RootElement, s_options).IsValid));
        }
        catch (JsonException)
        {
            return ValueTask.FromResult(DemoValidation.ToResult(false));
        }
    }

    private static JsonSchema InitializeNativeSchema()
    {
        if (!string.Equals(
            FlagshipAnnotatedArtifact.SchemaIdentity,
            GeneratedJsonSchemas.CanonicalArtifacts.FlagshipModel.GraphSha256,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The ASP.NET artifact and native JsonSchema.Net graph identities differ.");
        }

        Interlocked.Increment(ref s_initializationCount);
        return GeneratedJsonSchemas.FlagshipModel;
    }
}

internal sealed class CorvusProgramImageValidator :
    IOpenApiValidatedJsonSchemaValidator<FlagshipAnnotatedArtifact, CorvusProgramImageValidator>
{
    private static readonly Lazy<JsonSchemaEvaluator> s_evaluator = new(
        LoadForBinding,
        LazyThreadSafetyMode.ExecutionAndPublication);
    private static int s_initializationCount;

    public static string ConfigurationIdentity =>
        $"{FlagshipCorvusProgramImage.CorvusCommit};image-version={FlagshipCorvusProgramImage.ImageVersion};" +
        FlagshipCorvusProgramImage.ConfigurationIdentity;
    internal static int InitializationCount => Volatile.Read(ref s_initializationCount);

    public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsWellFormedJson(utf8Json.Span))
        {
            return ValueTask.FromResult(DemoValidation.ToResult(false));
        }

        return ValueTask.FromResult(DemoValidation.ToResult(s_evaluator.Value.Evaluate(utf8Json)));
    }

    private static bool IsWellFormedJson(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);
            while (reader.Read())
            {
            }

            return reader.CurrentDepth == 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonSchemaEvaluator LoadForBinding()
    {
        Interlocked.Increment(ref s_initializationCount);
        return DemoEngineEquivalence.LoadCorvusProgramImage();
    }
}

internal sealed class JsonSchemaNetBinding :
    IOpenApiValidatedJsonSchemaBinding<JsonSchemaNetBinding>
{
    public static Type Type => typeof(FlagshipModel);
    public static string SchemaIdentity => FlagshipAnnotatedArtifact.SchemaIdentity;
    public static string Identity { get; } = DemoBindingAdapter.CreateIdentity(
        "jsonschema-net",
        "9.4.0",
        JsonSchemaNetNativeValidator.ConfigurationIdentity);
    public static OpenApiJsonSchemaDialect Dialect => FlagshipAnnotatedArtifact.Dialect;
    public static OpenApiJsonSchemaValidationCapabilities Capabilities => FlagshipAnnotatedArtifact.Capabilities;

    public static OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion)
        => DemoBindingAdapter.CreateOpenApiSchema(openApiVersion, Identity);

    public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default)
        => JsonSchemaNetNativeValidator.ValidateAsync(utf8Json, purpose, cancellationToken);
}

internal sealed class CorvusBinding :
    IOpenApiValidatedJsonSchemaBinding<CorvusBinding>
{
    public static Type Type => typeof(FlagshipModel);
    public static string SchemaIdentity => FlagshipAnnotatedArtifact.SchemaIdentity;
    public static string Identity { get; } = CreateIdentity();
    public static OpenApiJsonSchemaDialect Dialect => FlagshipAnnotatedArtifact.Dialect;
    public static OpenApiJsonSchemaValidationCapabilities Capabilities => FlagshipAnnotatedArtifact.Capabilities;

    public static OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion)
        => DemoBindingAdapter.CreateOpenApiSchema(openApiVersion, Identity);

    public static ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default)
        => CorvusProgramImageValidator.ValidateAsync(utf8Json, purpose, cancellationToken);

    private static string CreateIdentity()
    {
        var identity = DemoBindingAdapter.CreateIdentity(
            FlagshipCorvusProgramImage.CorvusCommit,
            FlagshipCorvusProgramImage.ImageVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            FlagshipCorvusProgramImage.ConfigurationIdentity);
        if (!string.Equals(identity, FlagshipCorvusProgramImage.BindingIdentity, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The generated Corvus binding identity is inconsistent.");
        }

        return identity;
    }
}

internal static class DemoEngineEquivalence
{
    internal static JsonSchemaEvaluator LoadCorvusProgramImage()
    {
        if (!string.Equals(
            FlagshipAnnotatedArtifact.SchemaIdentity,
            FlagshipCorvusProgramImage.SchemaGraphIdentity,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The ASP.NET artifact and Corvus image schema identities differ.");
        }

        return JsonSchemaEvaluator.FromProgramImage(
            FlagshipCorvusProgramImage.Bytes,
            DemoCorvusOptions.Create());
    }
}

internal static class DemoCorvusOptions
{
    internal static JsonSchemaEvaluatorOptions Create()
        => new()
        {
            DefaultDialect = JsonSchemaDialect.Draft202012,
            AssertFormat = false,
            AssertContent = true,
            CompileRegularExpressions = false,
            BaseUri = "urn:flagship:conditional",
        };
}
