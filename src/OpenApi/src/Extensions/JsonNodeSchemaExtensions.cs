// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Provides a set of extension methods for modifying the opaque JSON Schema type
/// that is provided by the underlying schema generator in System.Text.Json.
/// </summary>
internal static class JsonNodeSchemaExtensions
{
    private static readonly Dictionary<Type, OpenApiSchema> _simpleTypeToOpenApiSchema = new()
    {
        [typeof(bool)] = new() { Type = JsonSchemaType.Boolean },
        [typeof(byte)] = new() { Type = JsonSchemaType.Integer, Format = "uint8" },
        [typeof(byte[])] = new() { Type = JsonSchemaType.String, Format = "byte" },
        [typeof(int)] = new() { Type = JsonSchemaType.Integer, Format = "int32" },
        [typeof(uint)] = new() { Type = JsonSchemaType.Integer, Format = "uint32" },
        [typeof(long)] = new() { Type = JsonSchemaType.Integer, Format = "int64" },
        [typeof(ulong)] = new() { Type = JsonSchemaType.Integer, Format = "uint64" },
        [typeof(short)] = new() { Type = JsonSchemaType.Integer, Format = "int16" },
        [typeof(ushort)] = new() { Type = JsonSchemaType.Integer, Format = "uint16" },
        [typeof(float)] = new() { Type = JsonSchemaType.Number, Format = "float" },
        [typeof(double)] = new() { Type = JsonSchemaType.Number, Format = "double" },
        [typeof(decimal)] = new() { Type = JsonSchemaType.Number, Format = "double" },
        [typeof(DateTime)] = new() { Type = JsonSchemaType.String, Format = "date-time" },
        [typeof(DateTimeOffset)] = new() { Type = JsonSchemaType.String, Format = "date-time" },
        [typeof(Guid)] = new() { Type = JsonSchemaType.String, Format = "uuid" },
        [typeof(char)] = new() { Type = JsonSchemaType.String, Format = "char" },
        [typeof(Uri)] = new() { Type = JsonSchemaType.String, Format = "uri" },
        [typeof(string)] = new() { Type = JsonSchemaType.String },
        [typeof(TimeOnly)] = new() { Type = JsonSchemaType.String, Format = "time" },
        [typeof(DateOnly)] = new() { Type = JsonSchemaType.String, Format = "date" },
    };

    /// <summary>
    /// Maps the given validation attributes to the target schema.
    /// </summary>
    /// <remarks>
    /// OpenApi schema v3 supports the validation vocabulary supported by JSON Schema. Because the underlying
    /// schema generator does not handle validation attributes to the validation vocabulary, we apply that mapping here.
    ///
    /// Note that this method targets <see cref="JsonNode"/> and not <see cref="OpenApiSchema"/> because it is
    /// designed to be invoked via the `OnGenerated` callback provided by the underlying schema generator
    /// so that attributes can be mapped to the properties associated with inputs and outputs to a given request.
    ///
    /// This implementation only supports mapping validation attributes that have an associated keyword in the
    /// validation vocabulary.
    ///
    /// Validation attributes are applied in a last-wins-order. For example, the following set of attributes:
    ///
    /// [Range(1, 10), Min(5)]
    ///
    /// will result in the schema having a minimum value of 5 and a maximum value of 10. This rule applies even
    /// though the model binding layer in MVC applies all validation attributes on an argument. The following
    /// set of attributes:
    ///
    /// [Base64String]
    /// [Url]
    /// public string Url { get; }
    ///
    /// will result in the schema having a type of "string" and a format of "uri" even though the model binding
    /// layer will validate the string against *both* constraints.
    /// </remarks>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="validationAttributes">A list of the validation attributes to apply.</param>
    internal static void ApplyValidationAttributes(this JsonNode schema, IEnumerable<Attribute> validationAttributes)
    {
        foreach (var attribute in validationAttributes)
        {
            if (attribute is Base64StringAttribute)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = "byte";
            }
            else if (attribute is RangeAttribute rangeAttribute)
            {
                // Use InvariantCulture if explicitly requested or if the range has been set via the
                // RangeAttribute(double, double) or RangeAttribute(int, int) constructors.
                var targetCulture = rangeAttribute.ParseLimitsInInvariantCulture || rangeAttribute.Minimum is double || rangeAttribute.Maximum is int
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.CurrentCulture;

                var minString = rangeAttribute.Minimum.ToString();
                var maxString = rangeAttribute.Maximum.ToString();

                if (decimal.TryParse(minString, NumberStyles.Any, targetCulture, out var minDecimal))
                {
                    schema[OpenApiSchemaKeywords.MinimumKeyword] = minDecimal;
                }
                if (decimal.TryParse(maxString, NumberStyles.Any, targetCulture, out var maxDecimal))
                {
                    schema[OpenApiSchemaKeywords.MaximumKeyword] = maxDecimal;
                }
            }
            else if (attribute is RegularExpressionAttribute regularExpressionAttribute)
            {
                schema[OpenApiSchemaKeywords.PatternKeyword] = regularExpressionAttribute.Pattern;
            }
            else if (attribute is MaxLengthAttribute maxLengthAttribute)
            {
                var targetKey = schema[OpenApiSchemaKeywords.TypeKeyword]?.GetValue<string>() == "array" ? OpenApiSchemaKeywords.MaxItemsKeyword : OpenApiSchemaKeywords.MaxLengthKeyword;
                schema[targetKey] = maxLengthAttribute.Length;
            }
            else if (attribute is MinLengthAttribute minLengthAttribute)
            {
                var targetKey = schema[OpenApiSchemaKeywords.TypeKeyword]?.GetValue<string>() == "array" ? OpenApiSchemaKeywords.MinItemsKeyword : OpenApiSchemaKeywords.MinLengthKeyword;
                schema[targetKey] = minLengthAttribute.Length;
            }
            else if (attribute is LengthAttribute lengthAttribute)
            {
                var targetKeySuffix = schema[OpenApiSchemaKeywords.TypeKeyword]?.GetValue<string>() == "array" ? "Items" : "Length";
                schema[$"min{targetKeySuffix}"] = lengthAttribute.MinimumLength;
                schema[$"max{targetKeySuffix}"] = lengthAttribute.MaximumLength;
            }
            else if (attribute is UrlAttribute)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = "uri";
            }
            else if (attribute is StringLengthAttribute stringLengthAttribute)
            {
                schema[OpenApiSchemaKeywords.MinLengthKeyword] = stringLengthAttribute.MinimumLength;
                schema[OpenApiSchemaKeywords.MaxLengthKeyword] = stringLengthAttribute.MaximumLength;
            }
        }
    }

    /// <summary>
    /// Populate the default value into the current schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="defaultValue">An object representing the <see cref="object"/> associated with the default value.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> associated with the target type.</param>
    internal static void ApplyDefaultValue(this JsonNode schema, object? defaultValue, JsonTypeInfo? jsonTypeInfo)
    {
        if (jsonTypeInfo is null)
        {
            return;
        }

        if (defaultValue is null)
        {
            schema[OpenApiSchemaKeywords.DefaultKeyword] = null;
        }
        else
        {
            schema[OpenApiSchemaKeywords.DefaultKeyword] = JsonSerializer.SerializeToNode(defaultValue, jsonTypeInfo);
        }
    }

    /// <summary>
    /// Applies the primitive types and formats to the schema based on the type.
    /// </summary>
    /// <remarks>
    /// OpenAPI v3 requires support for the format keyword in generated types. Because the
    /// underlying schema generator does not support this, we need to manually apply the
    /// supported formats to the schemas associated with the generated type.
    ///
    /// Whereas JsonSchema represents nullable types via `type: ["string", "null"]`, OpenAPI
    /// v3 exposes a nullable property on the schema. This method will set the nullable property
    /// based on whether the underlying schema generator returned an array type containing "null" to
    /// represent a nullable type or if the type was denoted as nullable from our lookup cache.
    ///
    /// Note that this method targets <see cref="JsonNode"/> and not <see cref="OpenApiSchema"/> because
    /// it is is designed to be invoked via the `OnGenerated` callback in the underlying schema generator as
    /// opposed to after the generated schemas have been mapped to OpenAPI schemas.
    /// </remarks>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="context">The <see cref="JsonSchemaExporterContext"/> associated with the <see paramref="schema"/>.</param>
    /// <param name="createSchemaReferenceId">A delegate that generates the reference ID to create for a type.</param>
    internal static void ApplyPrimitiveTypesAndFormats(this JsonNode schema, JsonSchemaExporterContext context, Func<JsonTypeInfo, string?> createSchemaReferenceId)
    {
        var type = context.TypeInfo.Type;
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (_simpleTypeToOpenApiSchema.TryGetValue(underlyingType ?? type, out var openApiSchema))
        {
            schema[OpenApiSchemaKeywords.NullableKeyword] = openApiSchema.Nullable || (schema[OpenApiSchemaKeywords.TypeKeyword] is JsonArray schemaType && schemaType.GetValues<string>().Contains("null"));
            schema[OpenApiSchemaKeywords.TypeKeyword] = openApiSchema.Type.ToString();
            schema[OpenApiSchemaKeywords.FormatKeyword] = openApiSchema.Format;
            schema[OpenApiConstants.SchemaId] = createSchemaReferenceId(context.TypeInfo);
            schema[OpenApiSchemaKeywords.NullableKeyword] = underlyingType != null;
            // Clear out patterns that the underlying JSON schema generator uses to represent
            // validations for DateTime, DateTimeOffset, and integers.
            schema[OpenApiSchemaKeywords.PatternKeyword] = null;
        }
    }

    /// <summary>
    /// Applies route constraints to the target schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="constraints">The list of <see cref="IRouteConstraint"/>s associated with the route parameter.</param>
    internal static void ApplyRouteConstraints(this JsonNode schema, IEnumerable<IRouteConstraint> constraints)
    {
        // Apply constraints in reverse order because when it comes to the routing
        // layer the first constraint that is violated causes routing to short circuit.
        foreach (var constraint in Enumerable.Reverse(constraints))
        {
            if (constraint is MinRouteConstraint minRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MinimumKeyword] = minRouteConstraint.Min;
            }
            else if (constraint is MaxRouteConstraint maxRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MaximumKeyword] = maxRouteConstraint.Max;
            }
            else if (constraint is MinLengthRouteConstraint minLengthRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MinLengthKeyword] = minLengthRouteConstraint.MinLength;
            }
            else if (constraint is MaxLengthRouteConstraint maxLengthRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MaxLengthKeyword] = maxLengthRouteConstraint.MaxLength;
            }
            else if (constraint is RangeRouteConstraint rangeRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MinimumKeyword] = rangeRouteConstraint.Min;
                schema[OpenApiSchemaKeywords.MaximumKeyword] = rangeRouteConstraint.Max;
            }
            else if (constraint is RegexRouteConstraint regexRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = null;
                schema[OpenApiSchemaKeywords.PatternKeyword] = regexRouteConstraint.Constraint.ToString();
            }
            else if (constraint is LengthRouteConstraint lengthRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.MinLengthKeyword] = lengthRouteConstraint.MinLength;
                schema[OpenApiSchemaKeywords.MaxLengthKeyword] = lengthRouteConstraint.MaxLength;
            }
            else if (constraint is FloatRouteConstraint or DecimalRouteConstraint or DoubleRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.Number.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = constraint is FloatRouteConstraint ? "float" : "double";
            }
            else if (constraint is LongRouteConstraint or IntRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.Integer.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = constraint is LongRouteConstraint ? "int64" : "int32";
            }
            else if (constraint is GuidRouteConstraint or StringRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = constraint is GuidRouteConstraint ? "uuid" : null;
            }
            else if (constraint is BoolRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.Boolean.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = null;
            }
            else if (constraint is AlphaRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = null;
            }
            else if (constraint is DateTimeRouteConstraint)
            {
                schema[OpenApiSchemaKeywords.TypeKeyword] = JsonSchemaType.String.ToString();
                schema[OpenApiSchemaKeywords.FormatKeyword] = "date-time";
            }
        }
    }

    /// <summary>
    /// Applies parameter-specific customizations to the target schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="parameterDescription">The <see cref="ApiParameterDescription"/> associated with the <see paramref="schema"/>.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> associated with the <see paramref="schema"/>.</param>
    internal static void ApplyParameterInfo(this JsonNode schema, ApiParameterDescription parameterDescription, JsonTypeInfo? jsonTypeInfo)
    {
        // This is special handling for parameters that are not bound from the body but represented in a complex type.
        // For example:
        //
        // public class MyArgs
        // {
        //     [Required]
        //     [Range(1, 10)]
        //     [FromQuery]
        //     public string Name { get; set; }
        // }
        //
        // public IActionResult(MyArgs myArgs) { }
        //
        // In this case, the `ApiParameterDescription` object that we received will represent the `Name` property
        // based on our model binding heuristics. In that case, to access the validation attributes that the
        // model binder will respect we will need to get the property from the container type and map the
        // attributes on it to the schema.
        if (parameterDescription.ModelMetadata is { PropertyName: { }, ContainerType: { }, HasValidators: true, ValidatorMetadata: { } validations })
        {
            var attributes = validations.OfType<ValidationAttribute>();
            schema.ApplyValidationAttributes(attributes);
        }
        if (parameterDescription.ParameterDescriptor is IParameterInfoParameterDescriptor { ParameterInfo: { } parameterInfo })
        {
            if (parameterInfo.HasDefaultValue)
            {
                schema.ApplyDefaultValue(parameterInfo.DefaultValue, jsonTypeInfo);
            }
            else if (parameterInfo.GetCustomAttributes<DefaultValueAttribute>().LastOrDefault() is { } defaultValueAttribute)
            {
                schema.ApplyDefaultValue(defaultValueAttribute.Value, jsonTypeInfo);
            }

            if (parameterInfo.GetCustomAttributes().OfType<ValidationAttribute>() is { } validationAttributes)
            {
                schema.ApplyValidationAttributes(validationAttributes);
            }

            schema.ApplyNullabilityContextInfo(parameterInfo);
        }
        // Route constraints are only defined on parameters that are sourced from the path. Since
        // they are encoded in the route template, and not in the type information based to the underlying
        // schema generator we have to handle them separately here.
        if (parameterDescription.RouteInfo?.Constraints is { } constraints)
        {
            schema.ApplyRouteConstraints(constraints);
        }

        if (parameterDescription.Source is { } bindingSource && SupportsNullableProperty(bindingSource))
        {
            schema[OpenApiSchemaKeywords.NullableKeyword] = false;
        }

        // Parameters sourced from the header, query, route, and/or form cannot be nullable based on our binding
        // rules but can be optional.
        static bool SupportsNullableProperty(BindingSource bindingSource) =>bindingSource == BindingSource.Header
            || bindingSource == BindingSource.Query
            || bindingSource == BindingSource.Path
            || bindingSource == BindingSource.Form
            || bindingSource == BindingSource.FormFile;
    }

    /// <summary>
    /// Applies the polymorphism options defined by System.Text.Json to the target schema following OpenAPI v3's
    /// conventions for the discriminator property.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="context">The <see cref="JsonSchemaExporterContext"/> associated with the current type.</param>
    /// <param name="createSchemaReferenceId">A delegate that generates the reference ID to create for a type.</param>
    internal static void MapPolymorphismOptionsToDiscriminator(this JsonNode schema, JsonSchemaExporterContext context, Func<JsonTypeInfo, string?> createSchemaReferenceId)
    {
        // The `context.BaseTypeInfo == null` check is used to ensure that we only apply the polymorphism options
        // to the top-level schema and not to any nested schemas that are generated.
        if (context.TypeInfo.PolymorphismOptions is { } polymorphismOptions && context.BaseTypeInfo == null)
        {
            // System.Text.Json supports serializing to a non-abstract base class if no discriminator is provided.
            // OpenAPI requires that all polymorphic sub-schemas have an associated discriminator. If the base type
            // doesn't declare itself as its own derived type via [JsonDerived], then it can't have a discriminator,
            // which OpenAPI requires. In that case, we exit early to avoid mapping the polymorphism options
            // to the `discriminator` property and return an un-discriminated `anyOf` schema instead.
            if (IsNonAbstractTypeWithoutDerivedTypeReference(context))
            {
                return;
            }
            var mappings = new JsonObject();
            foreach (var derivedType in polymorphismOptions.DerivedTypes)
            {
                if (derivedType.TypeDiscriminator is { } discriminator)
                {
                    var jsonDerivedType = context.TypeInfo.Options.GetTypeInfo(derivedType.DerivedType);
                    // Discriminator mappings are only supported in OpenAPI v3+ so we can safely assume that
                    // the generated reference mappings will support the OpenAPI v3 schema reference format
                    // that we hardcode here. We could use `OpenApiReference` to construct the reference and
                    // serialize it but we use a hardcoded string here to avoid allocating a new object and
                    // working around Microsoft.OpenApi's serialization libraries.
                    mappings[$"{discriminator}"] = $"#/components/schemas/{createSchemaReferenceId(context.TypeInfo)}{createSchemaReferenceId(jsonDerivedType)}";
                }
            }
            schema[OpenApiSchemaKeywords.DiscriminatorKeyword] = polymorphismOptions.TypeDiscriminatorPropertyName;
            schema[OpenApiSchemaKeywords.DiscriminatorMappingKeyword] = mappings;
        }
    }

    /// <summary>
    /// Set the x-schema-id property on the schema to the identifier associated with the type.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="context">The <see cref="JsonSchemaExporterContext"/> associated with the current type.</param>
    /// <param name="createSchemaReferenceId">A delegate that generates the reference ID to create for a type.</param>
    internal static void ApplySchemaReferenceId(this JsonNode schema, JsonSchemaExporterContext context, Func<JsonTypeInfo, string?> createSchemaReferenceId)
    {
        if (createSchemaReferenceId(context.TypeInfo) is { } schemaReferenceId)
        {
            schema[OpenApiConstants.SchemaId] = schemaReferenceId;
        }
        // If the type is a non-abstract base class that is not one of the derived types then mark it as a base schema.
        if (context.BaseTypeInfo == context.TypeInfo &&
            IsNonAbstractTypeWithoutDerivedTypeReference(context))
        {
            schema[OpenApiConstants.SchemaId] = "Base";
        }
    }

    /// <summary>
    /// Returns <langword ref="true" /> if the current type is a non-abstract base class that is not defined as its
    /// own derived type.
    /// </summary>
    /// <param name="context">The <see cref="JsonSchemaExporterContext"/> associated with the current type.</param>
    private static bool IsNonAbstractTypeWithoutDerivedTypeReference(JsonSchemaExporterContext context)
    {
        return !context.TypeInfo.Type.IsAbstract
            && context.TypeInfo.PolymorphismOptions is { } polymorphismOptions
            && !polymorphismOptions.DerivedTypes.Any(type => type.DerivedType == context.TypeInfo.Type);
    }

    /// <summary>
    /// Support applying nullability status for reference types provided as a parameter.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="parameterInfo">The <see cref="ParameterInfo" /> associated with the schema.</param>
    internal static void ApplyNullabilityContextInfo(this JsonNode schema, ParameterInfo parameterInfo)
    {
        if (parameterInfo.ParameterType.IsValueType)
        {
            return;
        }

        var nullabilityInfoContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityInfoContext.Create(parameterInfo);
        if (nullabilityInfo.WriteState == NullabilityState.Nullable)
        {
            schema[OpenApiSchemaKeywords.NullableKeyword] = true;
        }
    }

    /// <summary>
    /// Support applying nullability status for reference types provided as a property or field.
    /// </summary>
    /// <param name="schema">The <see cref="JsonNode"/> produced by the underlying schema generator.</param>
    /// <param name="propertyInfo">The <see cref="JsonPropertyInfo" /> associated with the schema.</param>
    internal static void ApplyNullabilityContextInfo(this JsonNode schema, JsonPropertyInfo propertyInfo)
    {
        // Avoid setting explicit nullability annotations for `object` types so they continue to match on the catch
        // all schema (no type, no format, no constraints).
        if (propertyInfo.PropertyType != typeof(object) && (propertyInfo.IsGetNullable || propertyInfo.IsSetNullable))
        {
            schema[OpenApiSchemaKeywords.NullableKeyword] = true;
        }
    }
}
