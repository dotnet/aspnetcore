// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Supports managing elements that belong in the "components" section of
/// an OpenAPI document. In particular, this is the API that is used to
/// interact with the JSON schemas that are managed by a given OpenAPI document.
/// </summary>
internal sealed class OpenApiSchemaService(
    [ServiceKey] string documentName,
    IOptions<JsonOptions> jsonOptions,
    IOptionsMonitor<OpenApiOptions> optionsMonitor)
{
    private readonly ConcurrentDictionary<Type, string?> _schemaIdCache = new();
    private readonly ConcurrentDictionary<(Type Type, InferredSchemaPurpose Purpose), InferredSchemaDocument> _inferredSchemaCache = new();
    private readonly ConditionalWeakTable<OpenApiDocument, InferredSchemaReferenceIdResolverSet> _inferredReferenceIdResolvers = new();
    private readonly AsyncLocal<HashSet<Type>?> _tupleSchemaExpansion = new();
    private readonly OpenApiJsonSchemaContext _jsonSchemaContext = new(new(jsonOptions.Value.SerializerOptions));
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(jsonOptions.Value.SerializerOptions)
    {
        // In order to properly handle the `RequiredAttribute` on type properties, add a modifier to support
        // setting `JsonPropertyInfo.IsRequired` based on the presence of the `RequiredAttribute`.
        TypeInfoResolver = jsonOptions.Value.SerializerOptions.TypeInfoResolver?.WithAddedModifier(jsonTypeInfo =>
        {
            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }
            foreach (var propertyInfo in jsonTypeInfo.Properties)
            {
                var hasRequiredAttribute = propertyInfo.AttributeProvider?
                    .GetCustomAttributes(inherit: false)
                    .Any(attr => attr is RequiredAttribute);
                propertyInfo.IsRequired |= hasRequiredAttribute ?? false;
            }
        })
    };

    private readonly ConcurrentDictionary<OpenApiSpecVersion, JsonSchemaExporterOptions> _configurations = new();

    private JsonSchemaExporterOptions CreateConfiguration(
        Func<JsonTypeInfo, string?> createSchemaReferenceId,
        bool useInferredComposition,
        OpenApiSpecVersion openApiVersion,
        InferredSchemaPurpose purpose = InferredSchemaPurpose.Neutral,
        Func<Type, Type, string?>? getPolymorphicReferenceId = null,
        InferredTransportBindingFact? rootTransportBindingFact = null)
    {
        JsonSchemaExporterOptions configuration = null!;
        configuration = new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, schema) =>
            {
                var type = context.TypeInfo.Type;
                var effectiveConverter = context.PropertyInfo?.CustomConverter ?? context.TypeInfo.Converter;
#pragma warning disable ASP0040 // The framework implements this experimental contract.
                var schemaEvidence = OpenApiSchemaEvidenceResolver.Resolve(
                    context.TypeInfo,
                    effectiveConverter,
                    purpose,
                    optionsMonitor.Get(documentName).SchemaEvidenceProviders,
                    context.PropertyInfo?.PropertyType);
                // Fix up schemas generated for IFormFile, IFormFileCollection, Stream, PipeReader,
                // FileContentResult, FileStreamResult, FileContentHttpResult and FileStreamHttpResult
                // that appear as properties within complex types.
                if (type == typeof(IFormFile) || type == typeof(Stream) || type == typeof(PipeReader)
                    || type == typeof(Mvc.FileContentResult) || type == typeof(Mvc.FileStreamResult)
                    || type == typeof(FileContentHttpResult) || type == typeof(FileStreamHttpResult))
                {
                    schema = new JsonObject
                    {
                        [OpenApiSchemaKeywords.TypeKeyword] = "string",
                        [OpenApiSchemaKeywords.FormatKeyword] = "binary",
                        [OpenApiConstants.SchemaId] = "IFormFile"
                    };
                }
                else if (type == typeof(IFormFileCollection))
                {
                    schema = new JsonObject
                    {
                        [OpenApiSchemaKeywords.TypeKeyword] = "array",
                        [OpenApiSchemaKeywords.ItemsKeyword] = new JsonObject
                        {
                            [OpenApiSchemaKeywords.TypeKeyword] = "string",
                            [OpenApiSchemaKeywords.FormatKeyword] = "binary",
                            [OpenApiConstants.SchemaId] = "IFormFile"
                        }
                    };
                }
                else if (type.IsJsonPatchDocument())
                {
                    schema = CreateSchemaForJsonPatch();
                }
                else if (schemaEvidence is OpenApiPositionalArraySchemaEvidence positionalArray)
                {
                    var expandedTupleTypes = _tupleSchemaExpansion.Value ??= [];
                    if (!expandedTupleTypes.Add(type))
                    {
                        schema = new JsonObject();
                    }
                    else
                    {
                        try
                        {
                            schema = CreateJsonArrayTupleSchema(
                                positionalArray,
                                configuration,
                                openApiVersion);
                        }
                        finally
                        {
                            expandedTupleTypes.Remove(type);
                            if (expandedTupleTypes.Count == 0)
                            {
                                _tupleSchemaExpansion.Value = null;
                            }
                        }
                    }
                }
                // STJ uses `true` in place of an empty object to represent a schema that matches
                // anything (like the `object` type) or types with user-defined converters. We override
                // this default behavior here to match the format expected in OpenAPI v3.
                if (schema.GetValueKind() == JsonValueKind.True)
                {
                    schema = new JsonObject();
                }
                var isScalarContract = false;
                if (useInferredComposition)
                {
                    var scalarFact = InferredScalarContractFactBuilder.Build(
                        context.TypeInfo,
                        context.PropertyInfo?.CustomConverter,
                        context.PropertyInfo?.AttributeProvider?.IsDefined(
                            typeof(System.Text.Json.Serialization.JsonConverterAttribute),
                            inherit: false)
                            is true,
                        schemaEvidence);
                    isScalarContract = scalarFact.IsScalar;
                    var scalarDecision = InferredScalarSchemaDecisionBuilder.Build(scalarFact);
                    if (rootTransportBindingFact is not null)
                    {
                        scalarDecision = scalarDecision with { Format = null };
                    }
                    else if (
                        scalarFact.Kind != InferredScalarContractKind.Base64String &&
                        scalarFact.IsScalar &&
                        !(schema[OpenApiSchemaKeywords.TypeKeyword] is JsonValue schemaType &&
                            schemaType.TryGetValue<string>(out var schemaTypeName) &&
                            schemaTypeName is "array" or "object"))
                    {
#pragma warning disable ASP0040 // The framework implements this experimental option.
                        var options = optionsMonitor.Get(documentName);
                        scalarDecision = scalarDecision with
                        {
                            Format = OpenApiScalarFormatResolver.ResolveJsonFormat(
                                options,
                                scalarFact,
                                context.TypeInfo.Type,
                                context.PropertyInfo is null
                                    ? OpenApiScalarFormatLocation.JsonBody
                                    : OpenApiScalarFormatLocation.JsonProperty,
                                purpose,
                                openApiVersion),
                        };
#pragma warning restore ASP0040
                    }
                    schema.ApplyInferredScalarDecision(
                        scalarDecision,
                        openApiVersion);
                    if (rootTransportBindingFact?.Source == InferredTransportBindingSource.Form &&
                        context.PropertyInfo is not null)
                    {
                        var formFact = InferredTransportBindingFactBuilder.Build(
                            context.TypeInfo.Type,
                            BindingSource.Form,
                            bindingMetadata: null);
#pragma warning disable ASP0040 // The framework implements this experimental option.
                        var options = optionsMonitor.Get(documentName);
                        var formDecision = InferredTransportSchemaDecisionBuilder.Build(
                            formFact,
                            fact => OpenApiScalarFormatResolver.ResolveTransportFormat(options, fact, openApiVersion));
#pragma warning restore ASP0040
                        schema.ApplyInferredTransportDecision(formDecision);
                    }
                }
                else
                {
                    schema.ApplyPrimitiveFormats(context);
                }
                schema.ApplySchemaReferenceId(context, createSchemaReferenceId);
#pragma warning disable ASP0040 // The framework implements this experimental option.
                if (useInferredComposition &&
                    optionsMonitor.Get(documentName).CreateScalarFormat is not null &&
                    isScalarContract &&
                    schema is JsonObject scalarSchema)
                {
                    scalarSchema.Remove(OpenApiConstants.SchemaId);
                }
                if (schemaEvidence is not null &&
                    context.PropertyInfo?.CustomConverter is not null &&
                    schema is JsonObject propertyContractSchema)
                {
                    propertyContractSchema.Remove(OpenApiConstants.SchemaId);
                }
#pragma warning restore ASP0040
                if (useInferredComposition)
                {
                    var inferredSchema = GetInferredSchema(type, purpose);
                    var inferredShape = inferredSchema[type];
                    schema.ApplyDirectionalObjectContract(
                        inferredShape,
                        purpose,
                        context.BaseTypeInfo?.PolymorphismOptions?.TypeDiscriminatorPropertyName);
                    var compositionDecision = inferredSchema.CompositionDecisions[type];
                    if (context.BaseTypeInfo is null)
                    {
                        schema.ApplyCompositionDecision(
                            compositionDecision,
                            getPolymorphicReferenceId ?? throw new InvalidOperationException(
                                Resources.InferredSchemaReferenceIdResolverUnavailable));
                    }
                    else
                    {
                        schema.MapPolymorphismOptionsToDiscriminator(context, createSchemaReferenceId);
                    }
                    schema.ApplyInheritanceCompositionDecision(inferredSchema, compositionDecision, createSchemaReferenceId, _jsonSerializerOptions);
                    schema.ApplyObjectContractDecision(
                        compositionDecision,
                        additionalPropertiesType => JsonSchemaExporter.GetJsonSchemaAsNode(
                            _jsonSerializerOptions,
                            additionalPropertiesType,
                            configuration));
                }
                else
                {
                    schema.MapPolymorphismOptionsToDiscriminator(context, createSchemaReferenceId);
                }
                if (context.PropertyInfo is { } jsonPropertyInfo)
                {
                    schema.ApplyNullabilityContextInfo(jsonPropertyInfo, useInferredComposition ? purpose : InferredSchemaPurpose.Neutral);
                }
                var underlyingType = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
                var typeAttributes = underlyingType.GetCustomAttributes(inherit: false);
                if (typeAttributes.OfType<DescriptionAttribute>().LastOrDefault() is { } typeDescriptionAttribute)
                {
                    schema[OpenApiSchemaKeywords.DescriptionKeyword] = typeDescriptionAttribute.Description;
                }
                if (typeAttributes.OfType<ObsoleteAttribute>().Any())
                {
                    schema[OpenApiSchemaKeywords.DeprecatedKeyword] = true;
                }
                if (context.PropertyInfo is { AttributeProvider: { } attributeProvider })
                {
                    var propertyAttributes = attributeProvider.GetCustomAttributes(inherit: false);
                    if (propertyAttributes.OfType<ValidationAttribute>() is { } validationAttributes)
                    {
                        schema.ApplyValidationAttributes(validationAttributes);
                    }
                    if (propertyAttributes.OfType<DefaultValueAttribute>().LastOrDefault() is { } defaultValueAttribute)
                    {
                        schema.ApplyDefaultValue(defaultValueAttribute.Value, context.TypeInfo);
                    }
                    var isInlinedSchema = !schema.WillBeComponentized();
                    if (isInlinedSchema)
                    {
                        if (propertyAttributes.OfType<DescriptionAttribute>().LastOrDefault() is { } descriptionAttribute)
                        {
                            schema[OpenApiSchemaKeywords.DescriptionKeyword] = descriptionAttribute.Description;
                        }
                        if (propertyAttributes.OfType<ObsoleteAttribute>().Any())
                        {
                            schema[OpenApiSchemaKeywords.DeprecatedKeyword] = true;
                        }
                    }
                    else
                    {
                        if (propertyAttributes.OfType<DescriptionAttribute>().LastOrDefault() is { } descriptionAttribute)
                        {
                            schema[OpenApiConstants.RefDescriptionAnnotation] = descriptionAttribute.Description;
                        }
                        if (propertyAttributes.OfType<ObsoleteAttribute>().Any())
                        {
                            schema[OpenApiConstants.RefDeprecatedAnnotation] = true;
                        }
                    }
                }
                schema.PruneNullTypeForComponentizedTypes();
                return schema;
            }
        };

        return configuration;
    }

#pragma warning disable ASP0040 // The framework implements this experimental contract.
    private JsonNode CreateJsonArrayTupleSchema(
        OpenApiPositionalArraySchemaEvidence evidence,
        JsonSchemaExporterOptions configuration,
        OpenApiSpecVersion openApiVersion)
    {
        var schema = new JsonObject
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "array",
            [OpenApiSchemaKeywords.MinItemsKeyword] = evidence.ElementTypes.Count,
            [OpenApiSchemaKeywords.MaxItemsKeyword] = evidence.ElementTypes.Count,
        };

        if (openApiVersion == OpenApiSpecVersion.OpenApi3_0)
        {
            schema[OpenApiSchemaKeywords.ItemsKeyword] = new JsonObject();
        }
        else
        {
            schema[OpenApiSchemaKeywords.PrefixItemsKeyword] = new JsonArray(
                evidence.ElementTypes
                    .Select(elementType => JsonSchemaExporter.GetJsonSchemaAsNode(
                        _jsonSerializerOptions,
                        elementType,
                        configuration))
                    .ToArray());
            schema[OpenApiSchemaKeywords.ItemsKeyword] = false;
        }

        return schema;
    }
#pragma warning restore ASP0040

    private static JsonObject CreateSchemaForJsonPatch()
    {
        var addReplaceTest = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path", "value"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["add", "replace", "test"]),
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
                ["value"] = new JsonObject()
            }
        };

        var moveCopy = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path", "from"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["move", "copy"]),
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
                ["from"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
            }
        };

        var remove = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["remove"])
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
            }
        };

        return new JsonObject
        {
            [OpenApiConstants.SchemaId] = "JsonPatchDocument",
            [OpenApiSchemaKeywords.TypeKeyword] = "array",
            [OpenApiSchemaKeywords.ItemsKeyword] = new JsonObject
            {
                [OpenApiSchemaKeywords.OneOfKeyword] = JsonArray([addReplaceTest, moveCopy, remove])
            },
        };

        // Using JsonArray inline causes the compile to pick the generic Add<T>() overload
        // which then generates native AoT warnings without adding a cost. To Avoid that use
        // this helper method that uses JsonNode to pick the native AoT compatible overload instead.
        static JsonArray JsonArray(ReadOnlySpan<JsonNode> values)
        {
            var array = new JsonArray();

            foreach (var value in values)
            {
                array.Add(value);
            }

            return array;
        }
    }

#pragma warning disable ASP0040 // The framework implements validated schema evidence.
    internal async Task<OpenApiSchema> GetOrCreateUnresolvedSchemaAsync(
        OpenApiDocument? document,
        Type type,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        OpenApiSpecVersion openApiVersion,
        InferredSchemaPurpose purpose = InferredSchemaPurpose.Neutral,
        ApiParameterDescription? parameterDescription = null,
        CancellationToken cancellationToken = default,
        InferredTransportBindingFact? transportBindingFact = null,
        OpenApiValidatedJsonSchemaRegistration? validatedSchema = null)
    {
#pragma warning disable ASP0040 // The framework implements validated schema evidence.
        if (validatedSchema is not null)
        {
            var importedSchema = OpenApiValidatedJsonSchemaImporter.Import(validatedSchema.Evidence, openApiVersion);
            await ApplyValidatedSchemaTransformersAsync(
                document ?? throw new InvalidOperationException("Validated schemas require an OpenAPI document."),
                importedSchema,
                type,
                scopedServiceProvider,
                schemaTransformers,
                openApiVersion,
                parameterDescription,
                cancellationToken);
            return importedSchema;
        }
#pragma warning restore ASP0040

        var schemaAsJsonObject = CreateSchema(type, document, openApiVersion, purpose, transportBindingFact);
        InferredTransportSchemaDecision? transportDecision = null;
        if (IsInferredMode && transportBindingFact is not null)
        {
#pragma warning disable ASP0040 // The framework implements this experimental option.
            var options = optionsMonitor.Get(documentName);
            transportDecision = InferredTransportSchemaDecisionBuilder.Build(
                transportBindingFact,
                fact => OpenApiScalarFormatResolver.ResolveTransportFormat(options, fact, openApiVersion));
#pragma warning restore ASP0040
            schemaAsJsonObject.ApplyInferredTransportDecision(transportDecision);
        }
        if (parameterDescription is not null)
        {
            schemaAsJsonObject.ApplyParameterInfo(parameterDescription, _jsonSerializerOptions.GetTypeInfo(type));
        }
        if (transportDecision is not null)
        {
            schemaAsJsonObject.ApplyInferredScalarFormat(transportDecision.Format);
        }
        if (IsInferredMode && transportBindingFact is not null)
        {
            schemaAsJsonObject.ApplyInferredTransportDefault(transportBindingFact);
        }
        // Use _jsonSchemaContext constructed from _jsonSerializerOptions to respect shared config set by end-user,
        // particularly in the case of maxDepth.
        var deserializedSchema = JsonSerializer.Deserialize(schemaAsJsonObject, _jsonSchemaContext.OpenApiJsonSchema);
        Debug.Assert(deserializedSchema != null, "The schema should have been deserialized successfully and materialize a non-null value.");
        var schema = deserializedSchema.Schema;
        await ApplySchemaTransformersAsync(document, schema, type, scopedServiceProvider, schemaTransformers, openApiVersion, purpose, parameterDescription, cancellationToken);
        return schema;
    }
#pragma warning restore ASP0040

#pragma warning disable ASP0040 // The framework implements validated schema evidence.
    internal async Task<IOpenApiSchema> GetOrCreateSchemaAsync(
        OpenApiDocument document,
        Type type,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        OpenApiSpecVersion openApiVersion,
        InferredSchemaPurpose purpose,
        ApiParameterDescription? parameterDescription = null,
        CancellationToken cancellationToken = default,
        InferredTransportBindingFact? transportBindingFact = null,
        OpenApiValidatedJsonSchemaRegistration? validatedSchema = null)
    {
        // For non-body enum parameters, check if a naming policy transforms the enum values.
        // If so, skip componentization and return an inline schema with the original C# member
        // names (which Enum.TryParse accepts). The component schema keeps the naming-policy
        // values for body serialization.
        var inlineEnumParam = false;
        if (parameterDescription is { Source: { } source, Type: { } paramType }
            && IsNonBodyBindingSource(source)
            && (Nullable.GetUnderlyingType(paramType) ?? paramType) is { IsEnum: true } enumType)
        {
            var rawNode = CreateSchema(type, document, openApiVersion, purpose, transportBindingFact);
            if (rawNode[OpenApiSchemaKeywords.EnumKeyword] is JsonArray rawEnum && rawEnum.Count > 0)
            {
                var memberNames = Enum.GetNames(enumType);
                for (var i = 0; i < memberNames.Length && i < rawEnum.Count; i++)
                {
                    if (rawEnum[i]?.GetValue<string>() != memberNames[i])
                    {
                        inlineEnumParam = true;
                        break;
                    }
                }
            }
        }

        var schema = await GetOrCreateUnresolvedSchemaAsync(
            document,
            type,
            scopedServiceProvider,
            schemaTransformers,
            openApiVersion,
            purpose,
            parameterDescription,
            cancellationToken,
            transportBindingFact,
            validatedSchema);

        if (IsInferredMode &&
            transportBindingFact is not null &&
            InferredTransportSchemaDecisionBuilder.Build(transportBindingFact).IsKnown)
        {
            return schema;
        }
#pragma warning restore ASP0040

        if (inlineEnumParam)
        {
            // The schema was originally tagged for componentization (x-schema-id was set),
            // so ApplyDefaultValue stored the default in the x-ref-default metadata annotation
            // instead of the "default" keyword. Since we're now inlining this schema, promote
            // the annotation to the schema's Default property.
            if (schema.Metadata?.TryGetValue(OpenApiConstants.RefDefaultAnnotation, out var refDefault) == true
                && refDefault is JsonNode defaultNode)
            {
                schema.Default = defaultNode;
                schema.Metadata.Remove(OpenApiConstants.RefDefaultAnnotation);
            }

            return schema;
        }

        // Cache the root schema IDs since we expect to be called
        // on the same type multiple times within an API
        var baseSchemaId = IsInferredMode
            ? GetInferredReferenceIdResolver(document, purpose, type).GetReferenceId(_jsonSerializerOptions.GetTypeInfo(type))
            : _schemaIdCache.GetOrAdd(type, t =>
            {
                var jsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(t);
                return optionsMonitor.Get(documentName).CreateSchemaReferenceId(jsonTypeInfo);
            });

        return ResolveReferenceForSchema(document, schema, baseSchemaId);
    }

    private static bool IsNonBodyBindingSource(BindingSource bindingSource) => bindingSource == BindingSource.Header
        || bindingSource == BindingSource.Query
        || bindingSource == BindingSource.Path
        || bindingSource == BindingSource.Form
        || bindingSource == BindingSource.FormFile;

    internal static IOpenApiSchema ResolveReferenceForSchema(OpenApiDocument document, IOpenApiSchema inputSchema, string? rootSchemaId, string? baseSchemaId = null)
    {
        var schema = UnwrapOpenApiSchema(inputSchema);

        var isComponentizedSchema = schema.IsComponentizedSchema(out var schemaId);

        // When we register it, this will be the resulting reference
        OpenApiSchemaReference? resultSchemaReference = null;
        if (inputSchema is OpenApiSchema && isComponentizedSchema)
        {
            // STJ's JsonSchemaExporter omits "type": "object" on object branches of an anyOf
            // when EVERY branch is an object - factoring the keyword onto the parent instead.
            //
            // Since we lift the branch into a top-level #/components/schemas/* entry and replace it with a $ref
            // we need to ensure the schema has an explicit "type": "object" to avoid losing that information in the translation.
            if (schema.Type is null && schema.Properties is { Count: > 0 })
            {
                schema.Type = JsonSchemaType.Object;
            }

            var targetReferenceId = baseSchemaId is not null
                ? $"{baseSchemaId}{schemaId}"
                : schemaId;
            if (!string.IsNullOrEmpty(targetReferenceId))
            {
                if (!document.AddOpenApiSchemaByReference(targetReferenceId, schema, out resultSchemaReference))
                {
                    var replacedPlaceholder = false;
                    if (document.Components?.Schemas is { } componentSchemas &&
                        componentSchemas.TryGetValue(targetReferenceId, out var existingSchema) &&
                        existingSchema is OpenApiSchema
                        {
                            Metadata: not null
                        } existingOpenApiSchema &&
                        existingOpenApiSchema.Metadata.TryGetValue(OpenApiConstants.SchemaIsInferredBasePlaceholder, out var isPlaceholder) &&
                        isPlaceholder is true &&
                        schema.Metadata?.ContainsKey(OpenApiConstants.SchemaIsInferredBasePlaceholder) != true)
                    {
                        componentSchemas[targetReferenceId] = schema;
                        replacedPlaceholder = true;
                    }

                    if (!replacedPlaceholder)
                    {
                        // We already added this schema, so it has already been resolved.
                        return resultSchemaReference;
                    }
                }
            }
        }

        if (schema.AnyOf is { Count: > 0 })
        {
            // For union types, do not prefix branch components with the union's name.
            // Union case schemas are structurally identical to the standalone case type
            // (no `$type` discriminator like polymorphism adds), so they should reuse the
            // standalone component name (e.g. "Kitten") instead of producing a duplicate
            // component (e.g. "UnionPetKitten") with the same content.
            var branchPrefix = schema.IsUnion() ||
                schema.Metadata?.TryGetValue(OpenApiConstants.SchemaIsInferredPolymorphism, out var isInferredPolymorphism) == true &&
                isInferredPolymorphism is true
                    ? null
                    : schemaId;
            for (var i = 0; i < schema.AnyOf.Count; i++)
            {
                schema.AnyOf[i] = ResolveReferenceForSchema(document, schema.AnyOf[i], rootSchemaId, branchPrefix);
            }
        }

        ResolveDiscriminatorReferences(document, schema);

        if (schema.Properties is not null)
        {
            // Materialize the collection first because IDictionary<TKey, TValue> implementations
            // (e.g. SortedDictionary) may disallow modifying the collection while enumerating it.
            foreach (var (key, propertyValue) in schema.Properties.ToList())
            {
                var resolvedProperty = ResolveReferenceForSchema(document, propertyValue, rootSchemaId);
                if (propertyValue is OpenApiSchema targetSchema &&
                    targetSchema.Metadata?.TryGetValue(OpenApiConstants.NullableProperty, out var isNullableProperty) == true &&
                    isNullableProperty is true)
                {
                    schema.Properties[key] = resolvedProperty.CreateOneOfNullableWrapper();
                }
                else
                {
                    schema.Properties[key] = resolvedProperty;
                }
            }
        }

        if (schema.AllOf is { Count: > 0 })
        {
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                schema.AllOf[i] = ResolveReferenceForSchema(document, schema.AllOf[i], rootSchemaId);
            }
        }

        if (schema.OneOf is { Count: > 0 })
        {
            for (var i = 0; i < schema.OneOf.Count; i++)
            {
                schema.OneOf[i] = ResolveReferenceForSchema(document, schema.OneOf[i], rootSchemaId);
            }
        }

        if (schema.AdditionalProperties is not null)
        {
            schema.AdditionalProperties = ResolveReferenceForSchema(document, schema.AdditionalProperties, rootSchemaId);
        }

        if (schema.Items is not null)
        {
            schema.Items = ResolveReferenceForSchema(document, schema.Items, rootSchemaId);
        }

        if (schema.Metadata?.TryGetValue(OpenApiConstants.SchemaPrefixItems, out var prefixItemsValue) == true &&
            prefixItemsValue is IOpenApiSchema[] prefixItems)
        {
            for (var i = 0; i < prefixItems.Length; i++)
            {
                prefixItems[i] = ResolveReferenceForSchema(document, prefixItems[i], rootSchemaId);
            }

            SynchronizeTuplePrefixItems(schema, prefixItems);
        }

        if (schema.Not is not null)
        {
            schema.Not = ResolveReferenceForSchema(document, schema.Not, rootSchemaId);
        }

        if (resultSchemaReference is not null)
        {
            return resultSchemaReference;
        }

        return schema;
    }

    private static void SynchronizeTuplePrefixItems(OpenApiSchema schema, IReadOnlyList<IOpenApiSchema> prefixItems)
    {
        var rawPrefixItems = new JsonArray();
        foreach (var prefixItem in prefixItems)
        {
            using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
            var openApiWriter = new OpenApiJsonWriter(textWriter);
            prefixItem.SerializeAsV31(openApiWriter);
            rawPrefixItems.Add(JsonNode.Parse(textWriter.ToString()));
        }

        schema.UnrecognizedKeywords ??= new Dictionary<string, JsonNode>();
        schema.UnrecognizedKeywords[OpenApiSchemaKeywords.PrefixItemsKeyword] = rawPrefixItems;
    }

    private static void ResolveDiscriminatorReferences(OpenApiDocument document, OpenApiSchema schema)
    {
        if (schema.Discriminator is not { } discriminator)
        {
            return;
        }

        if (discriminator.DefaultMapping is { } defaultMapping)
        {
            discriminator.DefaultMapping = ResolveSchemaReference(document, defaultMapping);
        }

        if (discriminator.Mapping is not null)
        {
            foreach (var mapping in discriminator.Mapping.ToArray())
            {
                discriminator.Mapping[mapping.Key] = ResolveSchemaReference(document, mapping.Value);
            }
        }
    }

    private static OpenApiSchemaReference ResolveSchemaReference(OpenApiDocument document, OpenApiSchemaReference schemaReference)
    {
        if (schemaReference.Reference.Id is not { } referenceId)
        {
            return schemaReference;
        }

        const string componentsSchemasReferencePrefix = "#/components/schemas/";
        if (referenceId.StartsWith(componentsSchemasReferencePrefix, StringComparison.Ordinal))
        {
            referenceId = referenceId[componentsSchemasReferencePrefix.Length..];
        }

        return new OpenApiSchemaReference(referenceId, document);
    }

    private static OpenApiSchema UnwrapOpenApiSchema(IOpenApiSchema sourceSchema)
    {
        if (sourceSchema is OpenApiSchemaReference schemaReference)
        {
            if (schemaReference.Target is OpenApiSchema target)
            {
                return target;
            }
            else
            {
                throw new InvalidOperationException($"The input schema must be an {nameof(OpenApiSchema)} or {nameof(OpenApiSchemaReference)}.");
            }
        }
        else if (sourceSchema is OpenApiSchema directSchema)
        {
            return directSchema;
        }
        else
        {
            throw new InvalidOperationException($"The input schema must be an {nameof(OpenApiSchema)} or {nameof(OpenApiSchemaReference)}.");
        }
    }

    internal async Task ApplySchemaTransformersAsync(
        OpenApiDocument? document,
        IOpenApiSchema schema,
        Type type,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        OpenApiSpecVersion openApiVersion,
        InferredSchemaPurpose purpose = InferredSchemaPurpose.Neutral,
        ApiParameterDescription? parameterDescription = null,
        CancellationToken cancellationToken = default)
    {
        if (schemaTransformers.Length == 0)
        {
            return;
        }

        var inferredSchema = GetInferredSchema(type, IsInferredMode ? purpose : InferredSchemaPurpose.Neutral);
        var jsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(type);
#pragma warning disable ASP0040 // The framework populates this experimental property.
        var context = new OpenApiSchemaTransformerContext
        {
            DocumentName = documentName,
            OpenApiVersion = openApiVersion,
            JsonTypeInfo = jsonTypeInfo,
            JsonPropertyInfo = null,
            ParameterDescription = parameterDescription,
            ApplicationServices = scopedServiceProvider,
            Document = document,
            SchemaTransformers = schemaTransformers
        };
#pragma warning restore ASP0040
        for (var i = 0; i < schemaTransformers.Length; i++)
        {
            // Reset context object to base state before running each transformer.
            var transformer = schemaTransformers[i];
            await InnerApplySchemaTransformersAsync(schema, inferredSchema, jsonTypeInfo, null, context, transformer, cancellationToken);
        }
    }

#pragma warning disable ASP0040 // The framework implements this experimental contract.
    private async Task ApplyValidatedSchemaTransformersAsync(
        OpenApiDocument document,
        OpenApiSchema schema,
        Type type,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        OpenApiSpecVersion openApiVersion,
        ApiParameterDescription? parameterDescription,
        CancellationToken cancellationToken)
    {
        if (schemaTransformers.Length == 0)
        {
            return;
        }

        var context = new OpenApiSchemaTransformerContext
        {
            DocumentName = documentName,
            OpenApiVersion = openApiVersion,
            JsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(type),
            JsonPropertyInfo = null,
            ParameterDescription = parameterDescription,
            ApplicationServices = scopedServiceProvider,
            Document = document,
            SchemaTransformers = schemaTransformers,
        };
        foreach (var transformer in schemaTransformers)
        {
            await ApplyValidatedSchemaTransformerAsync(schema, context, transformer, cancellationToken);
        }
    }
#pragma warning restore ASP0040

    private static async Task ApplyValidatedSchemaTransformerAsync(
        IOpenApiSchema inputSchema,
        OpenApiSchemaTransformerContext context,
        IOpenApiSchemaTransformer transformer,
        CancellationToken cancellationToken)
    {
        if (inputSchema is not OpenApiSchema schema)
        {
            return;
        }

        await transformer.TransformAsync(schema, context, cancellationToken);
        foreach (var child in EnumerateValidatedSchemaChildren(schema))
        {
            await ApplyValidatedSchemaTransformerAsync(child, context, transformer, cancellationToken);
        }
    }

    private static IEnumerable<IOpenApiSchema> EnumerateValidatedSchemaChildren(OpenApiSchema schema)
    {
        foreach (var collection in new IEnumerable<IOpenApiSchema>?[]
        {
            schema.Definitions?.Values,
            schema.Properties?.Values,
            schema.PatternProperties?.Values,
            schema.DependentSchemas?.Values,
            schema.AllOf,
            schema.AnyOf,
            schema.OneOf,
        })
        {
            if (collection is not null)
            {
                foreach (var child in collection)
                {
                    yield return child;
                }
            }
        }

        foreach (var child in new[]
        {
            schema.Not,
            schema.Items,
            schema.Contains,
            schema.AdditionalProperties,
            schema.PropertyNames,
            schema.UnevaluatedPropertiesSchema,
            schema.ContentSchema,
            schema.If,
            schema.Then,
            schema.Else,
        })
        {
            if (child is not null)
            {
                yield return child;
            }
        }

        if (schema.Metadata?.TryGetValue(OpenApiConstants.SchemaPrefixItems, out var prefixItemsValue) == true &&
            prefixItemsValue is IOpenApiSchema[] prefixItems)
        {
            foreach (var prefixItem in prefixItems)
            {
                yield return prefixItem;
            }
        }
    }

    private async Task InnerApplySchemaTransformersAsync(IOpenApiSchema inputSchema,
        InferredSchemaDocument inferredSchema,
        JsonTypeInfo jsonTypeInfo,
        JsonPropertyInfo? jsonPropertyInfo,
        OpenApiSchemaTransformerContext context,
        IOpenApiSchemaTransformer transformer,
        CancellationToken cancellationToken = default)
    {
        context.UpdateJsonTypeInfo(jsonTypeInfo, jsonPropertyInfo);
        var schema = UnwrapOpenApiSchema(inputSchema);
        await transformer.TransformAsync(schema, context, cancellationToken);

        var alternativeDecision = inferredSchema.CompositionDecisions[jsonTypeInfo.Type].Alternatives;
#pragma warning disable ASP0040 // The framework implements this experimental option.
        var inferredMode = optionsMonitor.Get(documentName).SchemaGenerationMode == OpenApiSchemaGenerationMode.Inferred;
#pragma warning restore ASP0040
        var alternativeSchemas = alternativeDecision.Kind switch
        {
            InferredAlternativeCompositionKind.OneOf when inferredMode => schema.OneOf,
            InferredAlternativeCompositionKind.OneOf => schema.AnyOf,
            InferredAlternativeCompositionKind.AnyOf => schema.AnyOf,
            _ => null,
        };
        var traverseAlternativeBranches = jsonTypeInfo.PolymorphismOptions is not null ||
            inferredMode &&
            alternativeDecision.Source == InferredAlternativeSource.Union &&
            alternativeDecision.Kind == InferredAlternativeCompositionKind.OneOf;
        if (alternativeSchemas is { Count: > 0 } && traverseAlternativeBranches)
        {
            if (inferredMode && alternativeSchemas.Count < alternativeDecision.Branches.Count)
            {
                throw new InvalidOperationException(Resources.FormatInferredAlternativeBranchesMismatchGeneratedSchema(jsonTypeInfo.Type));
            }

            var branchCount = Math.Min(alternativeSchemas.Count, alternativeDecision.Branches.Count);
            for (var i = 0; i < branchCount; i++)
            {
                var derivedJsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(alternativeDecision.Branches[i].Identity.Type);
                await InnerApplySchemaTransformersAsync(alternativeSchemas[i], inferredSchema, derivedJsonTypeInfo, null, context, transformer, cancellationToken);
            }
        }

        // If the schema is an array but uses AnyOf or OneOf then ElementType is null
        if (schema.Items is not null && jsonTypeInfo.ElementType is not null)
        {
            var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(jsonTypeInfo.ElementType);
            await InnerApplySchemaTransformersAsync(schema.Items, inferredSchema, elementTypeInfo, null, context, transformer, cancellationToken);
        }

#pragma warning disable ASP0040 // The framework implements this experimental contract.
        var schemaEvidence = OpenApiSchemaEvidenceResolver.Resolve(
            jsonTypeInfo,
            jsonPropertyInfo?.CustomConverter ?? jsonTypeInfo.Converter,
            inferredSchema.Purpose,
            optionsMonitor.Get(documentName).SchemaEvidenceProviders,
            jsonPropertyInfo?.PropertyType);
        if (schemaEvidence is OpenApiPositionalArraySchemaEvidence positionalArray &&
            schema.Metadata?.TryGetValue(OpenApiConstants.SchemaPrefixItems, out var prefixItemsValue) == true &&
            prefixItemsValue is IOpenApiSchema[] prefixItems)
        {
            if (prefixItems.Length != positionalArray.ElementTypes.Count)
            {
                throw new InvalidOperationException(Resources.FormatPositionalTupleElementsMismatchGeneratedSchema(jsonTypeInfo.Type));
            }

            for (var i = 0; i < prefixItems.Length; i++)
            {
                var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(positionalArray.ElementTypes[i]);
                await InnerApplySchemaTransformersAsync(prefixItems[i], inferredSchema, elementTypeInfo, null, context, transformer, cancellationToken);
            }

            SynchronizeTuplePrefixItems(schema, prefixItems);
        }
#pragma warning restore ASP0040

        var isInferredInheritance = inferredMode &&
            schema.Metadata?.TryGetValue(OpenApiConstants.SchemaIsInferredInheritance, out var inferredInheritance) == true &&
            inferredInheritance is true;
        if (isInferredInheritance || schema.Properties is { Count: > 0 })
        {
            foreach (var propertyInfo in jsonTypeInfo.Properties)
            {
                IOpenApiSchema? propertySchema;
                var hasPropertySchema = isInferredInheritance
                    ? TryGetComposedPropertySchema(schema, propertyInfo.Name, out propertySchema)
                    : schema.Properties!.TryGetValue(propertyInfo.Name, out propertySchema);
                if (hasPropertySchema && propertySchema is not null)
                {
                    var inferredProperty = inferredSchema[jsonTypeInfo.Type].GetProperty(propertyInfo.Name);
                    var propertyTypeInfo = _jsonSerializerOptions.GetTypeInfo(inferredProperty.DeclaredPropertyType);
                    await InnerApplySchemaTransformersAsync(propertySchema, inferredSchema, propertyTypeInfo, propertyInfo, context, transformer, cancellationToken);
                }
            }
        }

        if (schema is { AdditionalPropertiesAllowed: true, AdditionalProperties: not null } &&
            jsonTypeInfo.ElementType is not null)
        {
            var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(jsonTypeInfo.ElementType);
            await InnerApplySchemaTransformersAsync(schema.AdditionalProperties, inferredSchema, elementTypeInfo, null, context, transformer, cancellationToken);
        }
        else if (inferredMode &&
            schema is { AdditionalPropertiesAllowed: true, AdditionalProperties: not null } &&
            inferredSchema[jsonTypeInfo.Type].ExtensionDataProperty is { } extensionDataProperty &&
            inferredSchema[jsonTypeInfo.Type].AdditionalPropertiesType is { } additionalPropertiesType)
        {
            var extensionDataJsonPropertyInfo = jsonTypeInfo.Properties.First(
                property => property.IsExtensionData &&
                    StringComparer.Ordinal.Equals(property.Name, extensionDataProperty.Identity.JsonName));
            var additionalPropertiesTypeInfo = _jsonSerializerOptions.GetTypeInfo(additionalPropertiesType.Identity.Type);
            await InnerApplySchemaTransformersAsync(
                schema.AdditionalProperties,
                inferredSchema,
                additionalPropertiesTypeInfo,
                extensionDataJsonPropertyInfo,
                context,
                transformer,
                cancellationToken);
        }
    }

    private static bool TryGetComposedPropertySchema(
        OpenApiSchema schema,
        string propertyName,
        out IOpenApiSchema propertySchema)
    {
        if (schema.Properties?.TryGetValue(propertyName, out propertySchema!) == true)
        {
            return true;
        }

        if (schema.AllOf is not null)
        {
            foreach (var branch in schema.AllOf)
            {
                if (TryGetComposedPropertySchema(UnwrapOpenApiSchema(branch), propertyName, out propertySchema))
                {
                    return true;
                }
            }
        }

        propertySchema = null!;
        return false;
    }

    internal void InitializeInferredReferenceIds(OpenApiDocument document, IEnumerable<InferredSchemaRoot> roots)
    {
        if (!IsInferredMode)
        {
            return;
        }

        _inferredReferenceIdResolvers.GetValue(document, _ => CreateInferredReferenceIdResolverSet(roots));
    }

    private JsonNode CreateSchema(
        Type type,
        OpenApiDocument? document,
        OpenApiSpecVersion openApiVersion,
        InferredSchemaPurpose purpose,
        InferredTransportBindingFact? transportBindingFact = null)
    {
        // We always create a oneOf nullable wrapper ourselves manually.
        var effectivePurpose = IsInferredMode ? purpose : InferredSchemaPurpose.Neutral;
        var inferredSchema = GetInferredSchema(type, effectivePurpose);
        JsonSchemaExporterOptions configuration;
        if (IsInferredMode)
        {
            var referenceIdResolver = document is null
                ? CreateInferredReferenceIdResolverSet([new(type, effectivePurpose)]).Get(effectivePurpose)
                : GetInferredReferenceIdResolver(document, effectivePurpose, type);
            configuration = CreateConfiguration(
                referenceIdResolver.GetReferenceId,
                useInferredComposition: true,
                openApiVersion,
                effectivePurpose,
                referenceIdResolver.GetPolymorphicReferenceId,
                transportBindingFact);
        }
        else
        {
            configuration = _configurations.GetOrAdd(
                openApiVersion,
                version => CreateConfiguration(
                    typeInfo => optionsMonitor.Get(documentName).CreateSchemaReferenceId(typeInfo),
                    useInferredComposition: false,
                    version,
                    InferredSchemaPurpose.Neutral));
        }

        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(_jsonSerializerOptions, inferredSchema.Root.Identity.Type, configuration);
        return ResolveReferences(schema, schema);
    }

    private InferredSchemaReferenceIdResolver GetInferredReferenceIdResolver(
        OpenApiDocument document,
        InferredSchemaPurpose purpose,
        Type fallbackRootType)
        => _inferredReferenceIdResolvers
            .GetValue(document, _ => CreateInferredReferenceIdResolverSet([new(fallbackRootType, purpose)]))
            .Get(purpose);

    private InferredSchemaReferenceIdResolverSet CreateInferredReferenceIdResolverSet(IEnumerable<InferredSchemaRoot> roots)
    {
        var options = optionsMonitor.Get(documentName);
        return InferredSchemaReferenceIdResolverSet.Create(
            roots,
            _jsonSerializerOptions,
            GetInferredSchema,
            options.CreateSchemaReferenceId,
            options.UsesDefaultSchemaReferenceId,
#pragma warning disable ASP0040 // The framework implements this experimental option.
            options.CreateScalarFormat is not null);
#pragma warning restore ASP0040
    }

    private bool IsInferredMode
    {
        get
        {
#pragma warning disable ASP0040 // The framework implements this experimental option.
            return optionsMonitor.Get(documentName).SchemaGenerationMode == OpenApiSchemaGenerationMode.Inferred;
#pragma warning restore ASP0040
        }
    }

    private InferredSchemaDocument GetInferredSchema(Type type, InferredSchemaPurpose purpose)
        => _inferredSchemaCache.GetOrAdd(
            (type, purpose),
            static (key, state) => InferredSchemaShapeBuilder.Build(
                state.SerializerOptions,
                key.Type,
                key.Purpose,
                state.Options.SchemaEvidenceProviders),
            (SerializerOptions: _jsonSerializerOptions, Options: optionsMonitor.Get(documentName)));

    private static JsonNode ResolveReferences(JsonNode node, JsonNode rootSchema)
    {
        return ResolveReferencesRecursive(node, rootSchema);
    }

    private static JsonNode ResolveReferencesRecursive(JsonNode node, JsonNode rootSchema)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject.TryGetPropertyValue(OpenApiConstants.RefKeyword, out var refNode) &&
                refNode is JsonValue refValue &&
                refValue.TryGetValue<string>(out var refString) &&
                refString.StartsWith(OpenApiConstants.RefPrefix, StringComparison.Ordinal))
            {
                try
                {
                    // Resolve the reference path to the actual schema content
                    // to avoid relative references
                    var resolvedNode = ResolveReference(refString, rootSchema);
                    if (resolvedNode != null)
                    {
                        return resolvedNode.DeepClone();
                    }
                }
                catch (InvalidOperationException)
                {
                    // If resolution fails due to invalid path, return the original reference
                    // This maintains backward compatibility while preventing crashes
                }

                // If resolution fails, return the original reference
                return node;
            }

            // Process all properties recursively
            var newObject = new JsonObject();
            foreach (var property in jsonObject)
            {
                if (property.Value != null)
                {
                    var processedValue = ResolveReferencesRecursive(property.Value, rootSchema);
                    newObject[property.Key] = processedValue?.DeepClone();
                }
                else
                {
                    newObject[property.Key] = null;
                }
            }
            return newObject;
        }
        else if (node is JsonArray jsonArray)
        {
            var newArray = new JsonArray();
            for (var i = 0; i < jsonArray.Count; i++)
            {
                if (jsonArray[i] != null)
                {
                    var processedValue = ResolveReferencesRecursive(jsonArray[i]!, rootSchema);
                    newArray.Add(processedValue?.DeepClone());
                }
                else
                {
                    newArray.Add(null);
                }
            }
            return newArray;
        }

        // Return non-$ref nodes as-is
        return node;
    }

    private static JsonNode? ResolveReference(string refPath, JsonNode rootSchema)
    {
        // The refPath is expected to be a JSON Pointer (RFC 6901)
        // https://www.rfc-editor.org/info/rfc6901/
        // It follows the URI Fragment Identifier Representation.
        if (string.IsNullOrWhiteSpace(refPath))
        {
            throw new InvalidOperationException("Reference path cannot be null or empty.");
        }

        if (!refPath.StartsWith(OpenApiConstants.RefPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Only fragment references (starting with '{OpenApiConstants.RefPrefix}') are supported. Found: {refPath}");
        }

        // We already checked that the path starts with '#'.
        var currentPath = refPath.AsSpan().Slice(OpenApiConstants.RefPrefix.Length);
        var currentNode = rootSchema;

        while (currentPath.Length > 0)
        {
            // https://www.rfc-editor.org/info/rfc6901/#section-3
            // json-pointer    = *( "/" reference-token )
            if (currentPath[0] != '/')
            {
                throw new InvalidOperationException($"Failed to resolve reference '{refPath}'. Expected '{currentPath}' to start with '/'");
            }

            var currentPathWithoutSlash = currentPath.Slice(1);
            var indexOfNextPath = currentPathWithoutSlash.IndexOf('/');

            var currentReferenceToken =
                indexOfNextPath == -1
                ? currentPathWithoutSlash
                : currentPathWithoutSlash.Slice(0, indexOfNextPath);

            var unescapedReferenceToken = ParseReferenceToken(currentReferenceToken);
            currentNode = EvaluateReferenceToken(unescapedReferenceToken, currentNode, refPath);

            currentPath = indexOfNextPath == -1
                ? ReadOnlySpan<char>.Empty
                : currentPathWithoutSlash.Slice(indexOfNextPath);
        }

        return currentNode;
    }

    private static string ParseReferenceToken(ReadOnlySpan<char> referenceToken)
    {
        // https://www.rfc-editor.org/info/rfc6901/#section-6
        var unescapedReferenceToken = Uri.UnescapeDataString(referenceToken.ToString());

        // https://www.rfc-editor.org/info/rfc6901/#section-4
        // Evaluation of each reference token begins by decoding any escaped
        // character sequence.  This is performed by first transforming any
        // occurrence of the sequence '~1' to '/', and then transforming any
        // occurrence of the sequence '~0' to '~'.  By performing the
        // substitutions in this order, an implementation avoids the error of
        // turning '~01' first into '~1' and then into '/', which would be
        // incorrect (the string '~01' correctly becomes '~1' after
        // transformation).
        //
        // NOTE: we unescape the possibly percent-encoded value even if
        // STJ doesn't correctly percent-encode the ref today.
        // See https://github.com/dotnet/runtime/issues/130162
        if (unescapedReferenceToken.Contains('~'))
        {
            // Not common case, performance isn't super important.
            return unescapedReferenceToken.Replace("~1", "/").Replace("~0", "~");
        }

        return unescapedReferenceToken;
    }

    private static JsonNode EvaluateReferenceToken(string unescapedReferenceToken, JsonNode currentNode, string fullJsonPointer)
    {
        if (currentNode is JsonObject currentObject)
        {
            // https://www.rfc-editor.org/info/rfc6901/#section-4
            // If the currently referenced value is a JSON object, the new
            // referenced value is the object member with the name identified by
            // the reference token.  The member name is equal to the token if it
            // has the same number of Unicode characters as the token and their
            // code points are byte-by-byte equal.  No Unicode character
            // normalization is performed.  If a referenced member name is not
            // unique in an object, the member that is referenced is undefined
            // and evaluation fails (see below).
            if (!currentObject.TryGetPropertyValue(unescapedReferenceToken, out var referencedValue) ||
                referencedValue is null)
            {
                throw new InvalidOperationException($"Failed to resolve reference '{fullJsonPointer}': property '{unescapedReferenceToken}' not found.");
            }

            return referencedValue;
        }

        if (currentNode is JsonArray currentArray)
        {
            // https://www.rfc-editor.org/info/rfc6901/#section-4
            // If the currently referenced value is a JSON array, the reference
            // token MUST contain either:
            //   - characters comprised of digits (see ABNF below; note that
            //     leading zeros are not allowed) that represent an unsigned
            //     base-10 integer value, making the new referenced value the
            //     array element with the zero-based index identified by the
            //     token, or
            //   - exactly the single character "-", making the new referenced
            //     value the (nonexistent) member after the last array element.
            //
            // The ABNF syntax for array indices is:
            // array-index = %x30 / ( %x31-39 *(%x30-39) )
            //               ; "0", or digits without a leading "0"
            //
            // Note that the use of the "-" character to index an array will always
            // result in such an error condition because by definition it refers to
            // a nonexistent array element.  Thus, applications of JSON Pointer need
            // to specify how that character is to be handled, if it is to be
            // useful.
            //
            // In our case, "-" doesn't seem to be useful so we will throw.
            if (!int.TryParse(unescapedReferenceToken, NumberStyles.None, CultureInfo.InvariantCulture, out var arrayIndex))
            {
                throw new InvalidOperationException($"Failed to resolve reference '{fullJsonPointer}': cannot navigate an array when the current token '{unescapedReferenceToken}' isn't a valid number");
            }

            if (unescapedReferenceToken.StartsWith('0', StringComparison.Ordinal) && unescapedReferenceToken.Length > 1)
            {
                throw new InvalidOperationException($"Failed to resolve reference '{fullJsonPointer}': array index '{unescapedReferenceToken}' has a leading zero, which is not allowed.");
            }

            return currentArray[arrayIndex]
                ?? throw new InvalidOperationException($"Failed to resolve reference '{fullJsonPointer}': array index '{arrayIndex}' was not found.");
        }

        throw new InvalidOperationException($"Failed to resolve reference '{fullJsonPointer}': Unexpected JsonNode '{currentNode.GetType()}'");
    }
}
