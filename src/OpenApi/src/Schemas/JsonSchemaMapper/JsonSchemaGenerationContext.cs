// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace JsonSchemaMapper;

/// <summary>
/// Defines the context in which a JSON schema within a type graph is being generated.
/// </summary>
#if EXPOSE_JSON_SCHEMA_MAPPER
public
#else
internal
#endif
    readonly struct JsonSchemaGenerationContext
{
    internal JsonSchemaGenerationContext(JsonTypeInfo typeInfo, Type? declaringType, JsonPropertyInfo? propertyInfo, ParameterInfo? parameterInfo)
    {
        TypeInfo = typeInfo;
        DeclaringType = declaringType;
        PropertyInfo = propertyInfo;
        ParameterInfo = parameterInfo;
    }

    /// <summary>
    /// The <see cref="JsonTypeInfo"/> for the type being processed.
    /// </summary>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary>
    /// The declaring type of the property or parameter being processed.
    /// </summary>
    public Type? DeclaringType { get; }

    /// <summary>
    /// The <see cref="JsonPropertyInfo"/> if the schema is being generated for a property.
    /// </summary>
    public JsonPropertyInfo? PropertyInfo { get; }

    /// <summary>
    /// The <see cref="System.Reflection.ParameterInfo"/> if a constructor parameter
    /// has been associated with the accompanying <see cref="PropertyInfo"/>.
    /// </summary>
    public ParameterInfo? ParameterInfo { get; }

    /// <summary>
    /// Checks if the type, property, or parameter has the specified attribute applied.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute to resolve.</typeparam>
    /// <param name="inherit">Whether to look up the hierarchy chain for the inherited custom attribute.</param>
    /// <returns>True if the attribute is defined by the current context.</returns>
    public bool IsDefined<TAttribute>(bool inherit = false)
        where TAttribute : Attribute =>
        GetCustomAttributes(typeof(TAttribute), inherit).Any();

    /// <summary>
    /// Checks if the type, property, or parameter has the specified attribute applied.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute to resolve.</typeparam>
    /// <param name="inherit">Whether to look up the hierarchy chain for the inherited custom attribute.</param>
    /// <returns>The first attribute resolved from the current context, or null.</returns>
    public TAttribute? GetAttribute<TAttribute>(bool inherit = false)
        where TAttribute : Attribute =>
        (TAttribute?)GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault();

    /// <summary>
    /// Resolves any custom attributes that might have been applied to the type, property, or parameter.
    /// </summary>
    /// <param name="type">The attribute type to resolve.</param>
    /// <param name="inherit">Whether to look up the hierarchy chain for the inherited custom attribute.</param>
    /// <returns>An enumerable of all custom attributes defined by the context.</returns>
    public IEnumerable<Attribute> GetCustomAttributes(Type type, bool inherit = false)
    {
        // Resolves attributes starting from the property, then the parameter, and finally the type itself.
        return GetAttrs(JsonSchemaMapper.ResolveAttributeProvider(DeclaringType, PropertyInfo))
            .Concat(GetAttrs(ParameterInfo))
            .Concat(GetAttrs(TypeInfo.Type))
            .Cast<Attribute>();

        object[] GetAttrs(ICustomAttributeProvider? provider) =>
            provider?.GetCustomAttributes(type, inherit) ?? Array.Empty<object>();
    }
}
