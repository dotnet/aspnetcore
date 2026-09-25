// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Identifies the serializer direction represented by recognized schema evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiSchemaEvidencePurpose
{
    /// <summary>
    /// The evidence is not associated with a specific serializer direction.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// The evidence represents request input.
    /// </summary>
    Input = 1,

    /// <summary>
    /// The evidence represents response output.
    /// </summary>
    Output = 2,
}

/// <summary>
/// Identifies the JSON value kind enforced by recognized scalar evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiScalarSchemaValueKind
{
    /// <summary>
    /// A JSON boolean.
    /// </summary>
    Boolean = 0,

    /// <summary>
    /// A JSON string.
    /// </summary>
    String = 1,

    /// <summary>
    /// A JSON integer.
    /// </summary>
    Integer = 2,

    /// <summary>
    /// A JSON number.
    /// </summary>
    Number = 3,
}

/// <summary>
/// Provides stable runtime serializer inputs used to recognize authoritative schema evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiSchemaEvidenceContext
{
    internal OpenApiSchemaEvidenceContext(
        Type type,
        JsonTypeInfo typeInfo,
        JsonConverter converter,
        OpenApiSchemaEvidencePurpose purpose)
    {
        Type = type;
        EffectiveType = Nullable.GetUnderlyingType(type) ?? type;
        TypeInfo = typeInfo;
        Converter = converter;
        Purpose = purpose;
    }

    /// <summary>
    /// Gets the declared CLR type associated with the schema evidence.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the effective non-nullable CLR type associated with the schema evidence.
    /// </summary>
    public Type EffectiveType { get; }

    /// <summary>
    /// Gets the effective <see cref="JsonTypeInfo"/> associated with the schema evidence.
    /// </summary>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the effective converter that must enforce any returned evidence.
    /// </summary>
    public JsonConverter Converter { get; }

    /// <summary>
    /// Gets the serializer direction represented by the schema evidence.
    /// </summary>
    public OpenApiSchemaEvidencePurpose Purpose { get; }
}

/// <summary>
/// Provides authoritative, runtime-enforced schema evidence before OpenAPI decisions are made.
/// </summary>
/// <remarks>
/// Implementations must return evidence only when the effective converter, parser, or another runtime
/// mechanism enforces it. Documentation-only declarations should use schema transformers.
/// </remarks>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiSchemaEvidenceProvider
{
    /// <summary>
    /// Gets the recognized runtime-enforced schema evidence, or <see langword="null"/> when this provider does not recognize it.
    /// </summary>
    /// <param name="context">The effective runtime serializer context.</param>
    /// <returns>The recognized runtime-enforced evidence, or <see langword="null"/>.</returns>
    OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context);
}

/// <summary>
/// Represents authoritative schema evidence enforced by a runtime mechanism.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class OpenApiSchemaEvidence
{
    private protected OpenApiSchemaEvidence()
    {
    }
}

/// <summary>
/// Represents strict scalar schema evidence enforced by runtime serialization.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiScalarSchemaEvidence : OpenApiSchemaEvidence
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiScalarSchemaEvidence"/>.
    /// </summary>
    /// <param name="valueKind">The enforced JSON value kind.</param>
    /// <param name="format">An optional policy-controlled format annotation candidate.</param>
    /// <param name="pattern">An optional enforced regular-expression pattern for strings.</param>
    /// <param name="minimum">An optional inclusive numeric minimum.</param>
    /// <param name="maximum">An optional inclusive numeric maximum.</param>
    public OpenApiScalarSchemaEvidence(
        OpenApiScalarSchemaValueKind valueKind,
        string? format = null,
        string? pattern = null,
        BigInteger? minimum = null,
        BigInteger? maximum = null)
    {
        if (valueKind is < OpenApiScalarSchemaValueKind.Boolean or > OpenApiScalarSchemaValueKind.Number)
        {
            throw new ArgumentOutOfRangeException(nameof(valueKind));
        }

        if (pattern is not null && valueKind != OpenApiScalarSchemaValueKind.String)
        {
            throw new ArgumentException(Resources.SchemaEvidencePatternRequiresString, nameof(pattern));
        }

        if ((minimum is not null || maximum is not null) &&
            valueKind is not OpenApiScalarSchemaValueKind.Integer and not OpenApiScalarSchemaValueKind.Number)
        {
            throw new ArgumentException(Resources.SchemaEvidenceBoundsRequireNumber, nameof(minimum));
        }

        if (minimum > maximum)
        {
            throw new ArgumentException(Resources.SchemaEvidenceMinimumExceedsMaximum, nameof(minimum));
        }

        ValueKind = valueKind;
        Format = format;
        Pattern = pattern;
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Gets the enforced JSON value kind.
    /// </summary>
    public OpenApiScalarSchemaValueKind ValueKind { get; }

    /// <summary>
    /// Gets the optional policy-controlled format annotation candidate.
    /// </summary>
    public string? Format { get; }

    /// <summary>
    /// Gets the optional enforced regular-expression pattern.
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    /// Gets the optional inclusive numeric minimum.
    /// </summary>
    public BigInteger? Minimum { get; }

    /// <summary>
    /// Gets the optional inclusive numeric maximum.
    /// </summary>
    public BigInteger? Maximum { get; }
}

/// <summary>
/// Represents fixed-length positional JSON array evidence enforced by runtime serialization.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiPositionalArraySchemaEvidence : OpenApiSchemaEvidence
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenApiPositionalArraySchemaEvidence"/>.
    /// </summary>
    /// <param name="elementTypes">The CLR types serialized at each array position.</param>
    public OpenApiPositionalArraySchemaEvidence(IEnumerable<Type> elementTypes)
    {
        ArgumentNullException.ThrowIfNull(elementTypes);
        var elements = elementTypes.ToArray();
        if (Array.IndexOf(elements, null) >= 0)
        {
            throw new ArgumentException(Resources.SchemaEvidenceElementTypesCannotContainNull, nameof(elementTypes));
        }

        ElementTypes = new ReadOnlyCollection<Type>(elements);
    }

    /// <summary>
    /// Gets the CLR types serialized at each array position.
    /// </summary>
    public IReadOnlyList<Type> ElementTypes { get; }
}

/// <summary>
/// Identifies the JSON Schema dialect used by validated schema evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiJsonSchemaDialect
{
    /// <summary>
    /// JSON Schema Draft 2020-12.
    /// </summary>
    Draft202012 = 0,
}

/// <summary>
/// Identifies optional assertions enforced by a JSON Schema validator.
/// </summary>
[Flags]
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public enum OpenApiJsonSchemaValidationCapabilities
{
    /// <summary>
    /// No optional assertion vocabularies are enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// The validator treats recognized <c>format</c> values as assertions.
    /// </summary>
    FormatAssertions = 1,
}

/// <summary>
/// Represents an immutable, self-contained JSON Schema enforced by a registered runtime validator.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiValidatedJsonSchemaEvidence : OpenApiSchemaEvidence
{
    private const string Draft202012Dialect = "https://json-schema.org/draft/2020-12/schema";
    private readonly byte[] _utf8Schema;

    internal OpenApiValidatedJsonSchemaEvidence(
        ReadOnlyMemory<byte> utf8Schema,
        OpenApiJsonSchemaDialect dialect,
        OpenApiJsonSchemaValidationCapabilities validationCapabilities)
    {
        if (utf8Schema.IsEmpty)
        {
            throw new ArgumentException(Resources.ValidatedJsonSchemaCannotBeEmpty, nameof(utf8Schema));
        }

        _utf8Schema = utf8Schema.ToArray();
        using var document = JsonDocument.Parse(_utf8Schema);
        Schema = document.RootElement.Clone();
        if (Schema.ValueKind is not JsonValueKind.Object and not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException(Resources.ValidatedJsonSchemaMustBeSchema, nameof(utf8Schema));
        }
        if (Schema.ValueKind == JsonValueKind.Object &&
            (!Schema.TryGetProperty("$schema", out var schemaDialect) ||
             schemaDialect.ValueKind != JsonValueKind.String ||
             !string.Equals(schemaDialect.GetString(), Draft202012Dialect, StringComparison.Ordinal)))
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaDialectRequired(Draft202012Dialect), nameof(utf8Schema));
        }
        ValidateReferences(Schema, Schema);

        Dialect = dialect;
        ValidationCapabilities = validationCapabilities;
        Identity = Convert.ToHexStringLower(SHA256.HashData(_utf8Schema));
    }

    /// <summary>
    /// Gets the immutable JSON Schema document.
    /// </summary>
    public JsonElement Schema { get; }

    /// <summary>
    /// Gets the JSON Schema dialect.
    /// </summary>
    public OpenApiJsonSchemaDialect Dialect { get; }

    /// <summary>
    /// Gets a deterministic SHA-256 identity of the exact UTF-8 schema supplied to the validator.
    /// </summary>
    public string Identity { get; }

    /// <summary>
    /// Gets optional assertions enforced by the validator.
    /// </summary>
    public OpenApiJsonSchemaValidationCapabilities ValidationCapabilities { get; }

    internal ReadOnlyMemory<byte> Utf8Schema => _utf8Schema;

    private static void ValidateReferences(JsonElement root, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("$ref") &&
                    (property.Value.ValueKind != JsonValueKind.String ||
                     property.Value.GetString() is not { } reference ||
                     !TryResolveLocalReference(root, reference)))
                {
                    throw new ArgumentException(Resources.FormatValidatedJsonSchemaExternalReferenceNotSupported(
                        property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : string.Empty));
                }
                ValidateReferences(root, property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ValidateReferences(root, item);
            }
        }
    }

    private static bool TryResolveLocalReference(JsonElement root, string reference)
    {
        if (reference == "#")
        {
            return true;
        }
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return false;
        }

        var current = root;
        foreach (var rawSegment in reference.AsSpan(2).ToString().Split('/'))
        {
            var segment = Uri.UnescapeDataString(rawSegment)
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (current.ValueKind == JsonValueKind.Object &&
                current.TryGetProperty(segment, out var property))
            {
                current = property;
            }
            else if (current.ValueKind == JsonValueKind.Array &&
                int.TryParse(segment, out var index) &&
                index >= 0 &&
                index < current.GetArrayLength())
            {
                current = current[index];
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Provides context for validating a JSON payload.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiJsonSchemaValidationContext
{
    /// <summary>
    /// Initializes a validation context.
    /// </summary>
    public OpenApiJsonSchemaValidationContext(OpenApiValidatedJsonSchemaEvidence evidence, OpenApiSchemaEvidencePurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Evidence = evidence;
        Purpose = purpose;
    }

    /// <summary>
    /// Gets the schema evidence used for validation.
    /// </summary>
    public OpenApiValidatedJsonSchemaEvidence Evidence { get; }

    /// <summary>
    /// Gets whether the payload represents input or output.
    /// </summary>
    public OpenApiSchemaEvidencePurpose Purpose { get; }
}

/// <summary>
/// Represents one validator-neutral JSON Schema validation error.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiJsonSchemaValidationError
{
    /// <summary>
    /// Initializes a validation error.
    /// </summary>
    public OpenApiJsonSchemaValidationError(string instanceLocation, string keyword, string message)
    {
        ArgumentNullException.ThrowIfNull(instanceLocation);
        ArgumentNullException.ThrowIfNull(keyword);
        ArgumentNullException.ThrowIfNull(message);

        InstanceLocation = instanceLocation;
        Keyword = keyword;
        Message = message;
    }

    /// <summary>
    /// Gets the JSON Pointer location in the payload.
    /// </summary>
    public string InstanceLocation { get; }

    /// <summary>
    /// Gets the failed JSON Schema keyword.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Gets the client-safe validation message.
    /// </summary>
    public string Message { get; }
}

/// <summary>
/// Represents the result of JSON Schema validation.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public readonly struct OpenApiJsonSchemaValidationResult
{
    /// <summary>
    /// Initializes a successful validation result.
    /// </summary>
    public OpenApiJsonSchemaValidationResult()
    {
        Errors = null;
    }

    /// <summary>
    /// Initializes an invalid validation result.
    /// </summary>
    public OpenApiJsonSchemaValidationResult(IEnumerable<OpenApiJsonSchemaValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = new ReadOnlyCollection<OpenApiJsonSchemaValidationError>(errors.ToArray());
    }

    /// <summary>
    /// Gets a shared successful result that allocates no error collection.
    /// </summary>
    public static OpenApiJsonSchemaValidationResult Valid => default;

    /// <summary>
    /// Gets whether the payload is valid.
    /// </summary>
    public bool IsValid => Errors is null || Errors.Count == 0;

    /// <summary>
    /// Gets validation errors, or <see langword="null"/> when validation succeeds.
    /// </summary>
    public IReadOnlyList<OpenApiJsonSchemaValidationError>? Errors { get; }
}

/// <summary>
/// Validates raw UTF-8 JSON against a compiled schema.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiJsonSchemaValidator
{
    /// <summary>
    /// Validates a complete UTF-8 JSON payload.
    /// </summary>
    ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiJsonSchemaValidationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Compiles a validator for immutable JSON Schema evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiJsonSchemaValidatorFactory
{
    /// <summary>
    /// Creates a thread-safe validator that can validate many payloads.
    /// </summary>
    IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence);
}

/// <summary>
/// Configures endpoint enforcement for validated JSON Schema evidence.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiValidatedJsonSchemaOptions
{
    /// <summary>
    /// Gets or sets the maximum buffered payload size. Defaults to one MiB.
    /// </summary>
    public long MaxPayloadSize { get; init; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets whether an empty request body bypasses validation.
    /// </summary>
    public bool AllowEmptyRequestBody { get; init; }

    /// <summary>
    /// Gets or sets the response status code selected by output evidence. A null value matches any status.
    /// </summary>
    public int? ResponseStatusCode { get; init; }

    /// <summary>
    /// Gets or sets the JSON content type selected by the evidence. Defaults to <c>application/json</c>.
    /// </summary>
    public string ContentType { get; init; } = "application/json";
}

/// <summary>
/// Atomically associates validated JSON Schema evidence, its compiled validator, and endpoint enforcement.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class OpenApiValidatedJsonSchemaRegistration
{
    internal IOpenApiJsonSchemaValidator Validator { get; }
    internal OpenApiJsonSchemaValidationContext ValidationContext { get; }

    /// <summary>
    /// Initializes a validated schema registration.
    /// </summary>
    public OpenApiValidatedJsonSchemaRegistration(
        Type type,
        OpenApiSchemaEvidencePurpose purpose,
        ReadOnlyMemory<byte> utf8Schema,
        OpenApiJsonSchemaDialect dialect,
        OpenApiJsonSchemaValidationCapabilities validationCapabilities,
        IOpenApiJsonSchemaValidatorFactory validatorFactory,
        OpenApiValidatedJsonSchemaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(validatorFactory);
        if (purpose == OpenApiSchemaEvidencePurpose.Neutral)
        {
            throw new ArgumentException(Resources.ValidatedJsonSchemaPurposeMustBeDirectional, nameof(purpose));
        }

        Type = type;
        Purpose = purpose;
        Evidence = new(utf8Schema, dialect, validationCapabilities);
        Options = options ?? new();
        if (Options.MaxPayloadSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), Resources.ValidatedJsonSchemaPayloadLimitMustBePositive);
        }
        if (string.IsNullOrWhiteSpace(Options.ContentType))
        {
            throw new ArgumentException(Resources.ValidatedJsonSchemaContentTypeCannotBeEmpty, nameof(options));
        }

        Validator = validatorFactory.CreateValidator(Evidence) ??
            throw new InvalidOperationException(Resources.ValidatedJsonSchemaFactoryReturnedNull);
        ValidationContext = new(Evidence, purpose);
    }

    /// <summary>
    /// Gets the CLR type associated with the endpoint contract.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets whether this registration validates request input or response output.
    /// </summary>
    public OpenApiSchemaEvidencePurpose Purpose { get; }

    /// <summary>
    /// Gets the immutable validated schema evidence.
    /// </summary>
    public OpenApiValidatedJsonSchemaEvidence Evidence { get; }

    /// <summary>
    /// Gets the endpoint enforcement options.
    /// </summary>
    public OpenApiValidatedJsonSchemaOptions Options { get; }
}
