// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using JsonSchemaMapper;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Provides a set of extension methods for modifying the opaque JSON Schema type
/// that is provided by the underlying schema generator in System.Text.Json.
/// </summary>
internal static class JsonObjectSchemaExtensions
{
    private static readonly Dictionary<Type, OpenApiSchema> _simpleTypeToOpenApiSchema = new()
    {
        [typeof(bool)] = new() { Type = "boolean" },
        [typeof(byte)] = new() { Type = "string", Format = "byte" },
        [typeof(int)] = new() { Type = "integer", Format = "int32" },
        [typeof(uint)] = new() { Type = "integer", Format = "int32" },
        [typeof(long)] = new() { Type = "integer", Format = "int64" },
        [typeof(ulong)] = new() { Type = "integer", Format = "int64" },
        [typeof(short)] = new() { Type = "integer", Format = null },
        [typeof(ushort)] = new() { Type = "integer", Format = null },
        [typeof(float)] = new() { Type = "number", Format = "float" },
        [typeof(double)] = new() { Type = "number", Format = "double" },
        [typeof(decimal)] = new() { Type = "number", Format = "double" },
        [typeof(DateTime)] = new() { Type = "string", Format = "date-time" },
        [typeof(DateTimeOffset)] = new() { Type = "string", Format = "date-time" },
        [typeof(Guid)] = new() { Type = "string", Format = "uuid" },
        [typeof(char)] = new() { Type = "string" },
        [typeof(Uri)] = new() { Type = "string", Format = "uri" },
        [typeof(string)] = new() { Type = "string" },
    };

    /// <summary>
    /// Maps the given validation attributes to the target schema.
    /// </summary>
    /// <remarks>
    /// OpenApi schema v3 supports the validation vocabulary supported by JSON Schema. Because the underlying
    /// schema generator does not handle validation attributes to the validation vocabulary, we apply that mapping here.
    ///
    /// Note that this method targets <see cref="JsonObject"/> and not <see cref="OpenApiSchema"/> because it is
    /// designed to be invoked via the `OnGenerated` callback provided by the underlying schema generator
    /// so that attributes can be mapped to the properties associated with inputs and outputs to a given request.
    ///
    /// This implementation only supports mapping validation attributes that have an associated keyword in the
    /// validation vocabulary.
    /// </remarks>
    /// <param name="schema">The <see cref="JsonObject"/> produced by the underlying schema generator.</param>
    /// <param name="validationAttributes">A list of the validation attributes to apply.</param>
    internal static void ApplyValidationAttributes(this JsonObject schema, IEnumerable<Attribute> validationAttributes)
    {
        foreach (var attribute in validationAttributes)
        {
            if (attribute is Base64StringAttribute)
            {
                schema["type"] = "string";
                schema["format"] = "byte";
            }
            else if (attribute is RangeAttribute rangeAttribute)
            {
                schema["minimum"] = decimal.Parse(rangeAttribute.Minimum.ToString()!, CultureInfo.InvariantCulture);
                schema["maximum"] = decimal.Parse(rangeAttribute.Maximum.ToString()!, CultureInfo.InvariantCulture);
            }
            else if (attribute is RegularExpressionAttribute regularExpressionAttribute)
            {
                schema["pattern"] = regularExpressionAttribute.Pattern;
            }
            else if (attribute is MaxLengthAttribute maxLengthAttribute)
            {
                var targetKey = schema["type"]?.GetValue<string>() == "array" ? "maxItems" : "maxLength";
                schema[targetKey] = maxLengthAttribute.Length;
            }
            else if (attribute is MinLengthAttribute minLengthAttribute)
            {
                var targetKey = schema["type"]?.GetValue<string>() == "array" ? "minItems" : "minLength";
                schema[targetKey] = minLengthAttribute.Length;
            }
            else if (attribute is LengthAttribute lengthAttribute)
            {
                var targetKeySuffix = schema["type"]?.GetValue<string>() == "array" ? "Items" : "Length";
                schema[$"min{targetKeySuffix}"] = lengthAttribute.MinimumLength;
                schema[$"max{targetKeySuffix}"] = lengthAttribute.MaximumLength;
            }
            else if (attribute is UrlAttribute)
            {
                schema["type"] = "string";
                schema["format"] = "uri";
            }
            else if (attribute is StringLengthAttribute stringLengthAttribute)
            {
                schema["minLength"] = stringLengthAttribute.MinimumLength;
                schema["maxLength"] = stringLengthAttribute.MaximumLength;
            }
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
    /// Note that this method targets <see cref="JsonObject"/> and not <see cref="OpenApiSchema"/> because
    /// it is is designed to be invoked via the `OnGenerated` callback in the underlying schema generator as
    /// opposed to after the generated schemas have been mapped to OpenAPI schemas.
    /// </remarks>
    /// <param name="schema">The <see cref="JsonObject"/> produced by the underlying schema generator.</param>
    /// <param name="type">The <see cref="Type"/> associated with the <see paramref="schema"/>.</param>
    internal static void ApplyPrimitiveTypesAndFormats(this JsonObject schema, Type type)
    {
        if (_simpleTypeToOpenApiSchema.TryGetValue(type, out var openApiSchema))
        {
            schema["nullable"] = openApiSchema.Nullable || (schema["type"] is JsonArray schemaType && schemaType.GetValues<string>().Contains("null"));
            schema["type"] = openApiSchema.Type;
            schema["format"] = openApiSchema.Format;
        }
    }

    /// <summary>
    /// Applies route constraints to the target schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonObject"/> produced by the underlying schema generator.</param>
    /// <param name="constraints">The list of <see cref="IRouteConstraint"/>s associated with the route parameter.</param>
    internal static void ApplyRouteConstraints(this JsonObject schema, IEnumerable<IRouteConstraint> constraints)
    {
        // Apply constraints in reverse order because when it comes to the routing
        // layer the first constraint that is violated causes routing to short circuit.
        foreach (var constraint in constraints.Reverse())
        {
            if (constraint is MinRouteConstraint minRouteConstraint)
            {
                schema["minimum"] = minRouteConstraint.Min;
            }
            else if (constraint is MaxRouteConstraint maxRouteConstraint)
            {
                schema["maximum"] = maxRouteConstraint.Max;
            }
            else if (constraint is MinLengthRouteConstraint minLengthRouteConstraint)
            {
                schema["minLength"] = minLengthRouteConstraint.MinLength;
            }
            else if (constraint is MaxLengthRouteConstraint maxLengthRouteConstraint)
            {
                schema["maxLength"] = maxLengthRouteConstraint.MaxLength;
            }
            else if (constraint is RangeRouteConstraint rangeRouteConstraint)
            {
                schema["minimum"] = rangeRouteConstraint.Min;
                schema["maximum"] = rangeRouteConstraint.Max;
            }
            else if (constraint is RegexRouteConstraint regexRouteConstraint)
            {
                schema["type"] = "string";
                schema["format"] = null;
                schema["pattern"] = regexRouteConstraint.Constraint.ToString();
            }
            else if (constraint is LengthRouteConstraint lengthRouteConstraint)
            {
                schema["minLength"] = lengthRouteConstraint.MinLength;
                schema["maxLength"] = lengthRouteConstraint.MaxLength;
            }
            else if (constraint is FloatRouteConstraint or DecimalRouteConstraint or DoubleRouteConstraint)
            {
                schema["type"] = "number";
                schema["format"] = constraint is FloatRouteConstraint ? "float" : "double";
            }
            else if (constraint is LongRouteConstraint or IntRouteConstraint)
            {
                schema["type"] = "integer";
                schema["format"] = constraint is LongRouteConstraint ? "int64" : "int32";
            }
            else if (constraint is GuidRouteConstraint or StringRouteConstraint)
            {
                schema["type"] = "string";
                schema["format"] = constraint is GuidRouteConstraint ? "uuid" : null;
            }
            else if (constraint is BoolRouteConstraint)
            {
                schema["type"] = "boolean";
                schema["format"] = null;
            }
            else if (constraint is AlphaRouteConstraint)
            {
                schema["type"] = "string";
                schema["format"] = null;
            }
            else if (constraint is DateTimeRouteConstraint)
            {
                schema["type"] = "string";
                schema["format"] = "date-time";
            }
        }
    }

    /// <summary>
    /// Applies parameter-specific customizations to the target schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonObject"/> produced by the underlying schema generator.</param>
    /// <param name="parameterDescription">The <see cref="ApiParameterDescription"/> associated with the <see paramref="schema"/>.</param>
    internal static void ApplyParameterInfo(this JsonObject schema, ApiParameterDescription parameterDescription)
    {
        // Route constraints are only defined on parameters that are sourced from the path. Since
        // they are encoded in the route template, and not in the type information based to the underlying
        // schema generator we have to handle them separately here.
        if (parameterDescription.RouteInfo?.Constraints is { } constraints)
        {
            schema.ApplyRouteConstraints(constraints);
        }
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
        if (parameterDescription.ModelMetadata.PropertyName is { } propertyName)
        {
            var property = parameterDescription.ModelMetadata.ContainerType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property is not null)
            {
                var attributes = property.GetCustomAttributes(true).OfType<ValidationAttribute>();
                schema.ApplyValidationAttributes(attributes);
            }
        }
    }

    /// <summary>
    /// Applies the polymorphism options to the target schema following OpenAPI v3's conventions.
    /// </summary>
    /// <param name="schema">The <see cref="JsonObject"/> produced by the underlying schema generator.</param>
    /// <param name="context">The <see cref="JsonSchemaGenerationContext"/> associated with the current type.</param>
    internal static void ApplyPolymorphismOptions(this JsonObject schema, JsonSchemaGenerationContext context)
    {
        if (context.TypeInfo.PolymorphismOptions is { } polymorphismOptions)
        {
            var mappings = new JsonObject();
            foreach (var derivedType in polymorphismOptions.DerivedTypes)
            {
                if (derivedType.TypeDiscriminator is null)
                {
                    continue;
                }
                // TODO: Use the actual reference ID instead of the empty string.
                mappings[derivedType.TypeDiscriminator.ToString()!] = string.Empty;
            }
            schema["discriminatorPropertyName"] = polymorphismOptions.TypeDiscriminatorPropertyName;
            schema["discriminatorMappings"] = mappings;
        }
    }
}
