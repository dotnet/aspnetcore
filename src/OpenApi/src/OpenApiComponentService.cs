// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Json.Schema;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

/// <summary>
/// Manages resolving an OpenAPI schema for given types and maintaining the schema cache.
/// </summary>
internal class OpenApiComponentService(IOptions<JsonOptions> jsonOptions)
{
    private readonly Dictionary<Type, JsonSchemaBuilder> _typeToJsonSchema = new();
    private readonly JsonSerializerOptions _serializerOptions = jsonOptions.Value.SerializerOptions;
    private readonly IJsonTypeInfoResolver? _defaultJsonTypeInfoResolver = jsonOptions.Value.SerializerOptions.TypeInfoResolver;
    private static readonly JsonSchemaBuilder _stringJsonSchema = new JsonSchemaBuilder().Type(SchemaValueType.String);

    internal OpenApiComponents GetOpenApiComponents()
    {
        var components = new OpenApiComponents();
        foreach (var (type, schema) in _typeToJsonSchema)
        {
            components.Schemas.Add(GetReferenceId(type), schema.Build());
        }
        return components;
    }

    internal JsonSchemaBuilder GetOrCreateJsonSchemaForType(Type type, ApiParameterDescription? parameterDescription = null, bool skipPolymorphismCheck = false)
    {
        if (_defaultJsonTypeInfoResolver is null)
        {
            return _stringJsonSchema;
        }
        if (_typeToJsonSchema.TryGetValue(type, out var cachedSchema))
        {
            return cachedSchema;
        }
        var jsonType = _defaultJsonTypeInfoResolver.GetTypeInfo(type, _serializerOptions);
        if (jsonType == null)
        {
            return _stringJsonSchema;
        }
        var schemaBuilder = new JsonSchemaBuilder();
        var useRef = false;
        var addToCache = false;
        if (jsonType.Type == typeof(JsonNode))
        {
            schemaBuilder.Type(SchemaValueType.Object);
            schemaBuilder.AdditionalPropertiesAllowed(true);
            schemaBuilder.AdditionalProperties(new JsonSchemaBuilder().Type(SchemaValueType.Object).Build());
            return schemaBuilder;
        }
        if (jsonType.Kind == JsonTypeInfoKind.Dictionary)
        {
            schemaBuilder.Type(SchemaValueType.Object);
            schemaBuilder.AdditionalPropertiesAllowed(true);
            var genericTypeArgs = jsonType.Type.GetGenericArguments();
            Type? valueType = null;
            if (genericTypeArgs.Length == 2)
            {
                valueType = jsonType.Type.GetGenericArguments().Last();
            }
            schemaBuilder.AdditionalProperties(OpenApiTypeMapper.MapTypeToJsonPrimitiveType(valueType));
        }
        if (jsonType.Kind == JsonTypeInfoKind.None)
        {
            if (type.IsEnum)
            {
                var enumSchema = OpenApiTypeMapper.MapTypeToJsonPrimitiveType(type.GetEnumUnderlyingType());
                if (enumSchema.TryGetKeyword<TypeKeyword>(out var typeKeyword))
                {
                    schemaBuilder.Type(typeKeyword!.Type);
                }
                if (enumSchema.TryGetKeyword<AnyOfKeyword>(out var anyOfKeyword))
                {
                    schemaBuilder.AnyOf(anyOfKeyword!.Schemas);
                }
                foreach (var value in Enum.GetValues(type))
                {
                    schemaBuilder.Enum(JsonNode.Parse(JsonSerializer.Serialize(value)));
                }
            }
            else
            {
                var intermediarySchema = OpenApiTypeMapper.MapTypeToJsonPrimitiveType(type);
                if (intermediarySchema.TryGetKeyword<TypeKeyword>(out var typeKeyword))
                {
                    schemaBuilder.Type(typeKeyword!.Type);
                }
                if (intermediarySchema.TryGetKeyword<AnyOfKeyword>(out var anyOfKeyword))
                {
                    schemaBuilder.AnyOf(anyOfKeyword!.Schemas);
                }
                var defaultValueAttribute = jsonType.Type.GetCustomAttributes(true).OfType<DefaultValueAttribute>().FirstOrDefault();
                if (defaultValueAttribute != null && defaultValueAttribute.Value != null)
                {
                    schemaBuilder.Default(JsonValue.Create(defaultValueAttribute.Value));
                }
                if (parameterDescription != null && parameterDescription.DefaultValue != null && parameterDescription.DefaultValue.ToString() != "")
                {
                    schemaBuilder.Default(JsonValue.Create(parameterDescription.DefaultValue));
                }
            }
        }
        if (jsonType.Kind == JsonTypeInfoKind.Enumerable)
        {
            schemaBuilder.Type(SchemaValueType.Array);
            var elementType = jsonType.Type.GetElementType() ?? jsonType.Type.GetGenericArguments().First();
            schemaBuilder.Items(OpenApiTypeMapper.MapTypeToJsonPrimitiveType(elementType));
        }
        if (jsonType.Kind == JsonTypeInfoKind.Object)
        {
            if (!skipPolymorphismCheck && jsonType.PolymorphismOptions is { } polymorphismOptions && polymorphismOptions.DerivedTypes.Count > 0)
            {
                var discriminator = new OpenApiDiscriminator
                {
                    PropertyName = polymorphismOptions.TypeDiscriminatorPropertyName,
                    Mapping = new Dictionary<string, string>()
                };
                foreach (var derivedType in polymorphismOptions.DerivedTypes)
                {
                    schemaBuilder.OneOf(GetOrCreateJsonSchemaForType(derivedType.DerivedType));
                    if (derivedType.TypeDiscriminator != null)
                    {
                        discriminator.Mapping.Add(derivedType.TypeDiscriminator!.ToString(), GetReferenceId(derivedType.DerivedType));
                    }
                }
                schemaBuilder.Discriminator(discriminator);
            }
            else if (jsonType.Type.BaseType is { } baseType && baseType != typeof(object))
            {
                schemaBuilder.AllOf(GetOrCreateJsonSchemaForType(baseType, skipPolymorphismCheck: true));
            }
            addToCache = true;
            useRef = true;
            schemaBuilder.Type(SchemaValueType.Object);
            schemaBuilder.AdditionalPropertiesAllowed(false);
            var properties = new Dictionary<string, JsonSchema>();
            foreach (var property in jsonType.Properties)
            {
                if (jsonType.Type.GetProperty(property.Name) is { } propertyInfo && propertyInfo.DeclaringType != jsonType.Type)
                {
                    continue;
                }
                var innerSchema = GetOrCreateJsonSchemaForType(property.PropertyType);
                var defaultValueAttribute = property.AttributeProvider!.GetCustomAttributes(true).OfType<DefaultValueAttribute>().FirstOrDefault();
                if (defaultValueAttribute?.Value != null)
                {
                    innerSchema.Default(JsonValue.Create(defaultValueAttribute.Value));
                }
                innerSchema.ReadOnly(property.Set is null);
                innerSchema.WriteOnly(property.Get is null);
                ApplyValidationAttributes(property.AttributeProvider.GetCustomAttributes(true), innerSchema);
                properties.Add(property.Name, innerSchema.Build());

            }
            schemaBuilder.Properties(properties);
        }
        if (parameterDescription?.ParameterDescriptor is IParameterInfoParameterDescriptor parameterInfoParameterDescriptor)
        {
            ApplyValidationAttributes(parameterInfoParameterDescriptor.ParameterInfo.GetCustomAttributes(true), schemaBuilder);
        }
        if (parameterDescription?.RouteInfo?.Constraints is not null)
        {
            ApplyRouteConstraints(parameterDescription.RouteInfo.Constraints, schemaBuilder);
        }
        if (addToCache)
        {
            _typeToJsonSchema[type] = schemaBuilder;
        }
        if (useRef)
        {
            schemaBuilder.Ref(GetReferenceId(type));
        }
        return schemaBuilder;
    }

    private static void ApplyValidationAttributes(object[] attributes, JsonSchemaBuilder schemaBuilder)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.GetType().IsSubclassOf(typeof(ValidationAttribute)))
            {
                if (attribute is RangeAttribute rangeAttribute)
                {
                    schemaBuilder.Minimum(decimal.Parse(rangeAttribute.Minimum.ToString()!, CultureInfo.InvariantCulture));
                    schemaBuilder.Maximum(decimal.Parse(rangeAttribute.Maximum.ToString()!, CultureInfo.InvariantCulture));
                }
                if (attribute is RegularExpressionAttribute regularExpressionAttribute)
                {
                    schemaBuilder.Pattern(regularExpressionAttribute.Pattern);
                }
                if (attribute is MaxLengthAttribute maxLengthAttribute)
                {
                    schemaBuilder.MaxLength((uint)maxLengthAttribute.Length);
                }
                if (attribute is MinLengthAttribute minLengthAttribute)
                {
                    schemaBuilder.MinLength((uint)minLengthAttribute.Length);
                }
                if (attribute is StringLengthAttribute stringLengthAttribute)
                {
                    schemaBuilder.MinLength((uint)stringLengthAttribute.MinimumLength);
                    schemaBuilder.MaxLength((uint)stringLengthAttribute.MaximumLength);
                }
            }
        }
    }

    private static void ApplyRouteConstraints(IEnumerable<IRouteConstraint> constraints, JsonSchemaBuilder schemaBuilder)
    {
        foreach (var constraint in constraints)
        {
            if (constraint is MinRouteConstraint minRouteConstraint)
            {
                schemaBuilder.Minimum(minRouteConstraint.Min);
            }
            else if (constraint is MaxRouteConstraint maxRouteConstraint)
            {
                schemaBuilder.Maximum(maxRouteConstraint.Max);
            }
            else if (constraint is MinLengthRouteConstraint minLengthRouteConstraint)
            {
                schemaBuilder.MinLength((uint)minLengthRouteConstraint.MinLength);
            }
            else if (constraint is MaxLengthRouteConstraint maxLengthRouteConstraint)
            {
                schemaBuilder.MaxLength((uint)maxLengthRouteConstraint.MaxLength);
            }
            else if (constraint is RangeRouteConstraint rangeRouteConstraint)
            {
                schemaBuilder.Minimum(rangeRouteConstraint.Min);
                schemaBuilder.Maximum(rangeRouteConstraint.Max);
            }
            else if (constraint is RegexRouteConstraint regexRouteConstraint)
            {
                schemaBuilder.Pattern(regexRouteConstraint.Constraint.ToString());
            }
            else if (constraint is LengthRouteConstraint lengthRouteConstraint)
            {
                schemaBuilder.MinLength((uint)lengthRouteConstraint.MinLength);
                schemaBuilder.MaxLength((uint)lengthRouteConstraint.MaxLength);
            }
            else if (constraint is FloatRouteConstraint or DecimalRouteConstraint)
            {
                schemaBuilder.Type(SchemaValueType.Number);
            }
            else if (constraint is LongRouteConstraint or IntRouteConstraint)
            {
                schemaBuilder.Type(SchemaValueType.Integer);
            }
            else if (constraint is GuidRouteConstraint or StringRouteConstraint)
            {
                schemaBuilder.Type(SchemaValueType.String);
            }
            else if (constraint is BoolRouteConstraint)
            {
                schemaBuilder.Type(SchemaValueType.Boolean);
            }
        }
    }

    private string GetReferenceId(Type type)
    {
        if (!type.IsConstructedGenericType)
        {
            return type.Name.Replace("[]", "Array");
        }

        var prefix = type.GetGenericArguments()
            .Select(GetReferenceId)
            .Aggregate((previous, current) => previous + current);

        if (IsAnonymousType(type))
        {
            return prefix + "AnonymousType";
        }

        return prefix + type.Name.Split('`').First();
    }

    private static bool IsAnonymousType(Type type) => type.GetTypeInfo().IsClass
        && type.GetTypeInfo().IsDefined(typeof(CompilerGeneratedAttribute))
        && !type.IsNested
        && type.Name.StartsWith("<>", StringComparison.Ordinal)
        && type.Name.Contains("__Anonymous");
}
