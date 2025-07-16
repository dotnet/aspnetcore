#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Experimental runtime implementation of <see cref="IValidatableInfoResolver"/> for type validation.
/// </summary>
/// <remarks>
/// This is an experimental API and may change in future versions.
/// </remarks>
[RequiresUnreferencedCode("RuntimeValidatableTypeInfoResolver uses reflection to inspect types, properties, and attributes at runtime, including JsonDerivedTypeAttribute and record constructors. Trimming or AOT compilation may remove members required for validation.")]
[Experimental("ASP0029")]
public sealed class RuntimeValidatableTypeInfoResolver : IValidatableInfoResolver
{
    private static readonly ConcurrentDictionary<Type, IValidatableInfo?> _cache = new();

    /// <summary>
    /// Attempts to get validatable type information for the specified type using runtime reflection.
    /// </summary>
    /// <param name="type">The type to get validation information for.</param>
    /// <param name="info">When this method returns, contains the validatable type information if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if validatable type information was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValidatableTypeInfo(
        Type type,
        [NotNullWhen(true)] out IValidatableInfo? info)
    {
        if (_cache.TryGetValue(type, out info))
        {
            return info is not null;
        }

        info = CreateValidatableTypeInfo(type, new HashSet<Type>());
        _cache.TryAdd(type, info);
        return info is not null;
    }

    /// <summary>
    /// Attempts to get validatable parameter information for the specified parameter.
    /// </summary>
    /// <param name="parameterInfo">The parameter to get validation information for.</param>
    /// <param name="validatableInfo">When this method returns, contains the validatable parameter information if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if validatable parameter information was found; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This implementation always returns <see langword="false"/> as parameter resolution is handled by <see cref="RuntimeValidatableParameterInfoResolver"/>.
    /// </remarks>
    public bool TryGetValidatableParameterInfo(
        ParameterInfo parameterInfo,
        [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        validatableInfo = null;
        return false;
    }

    private static RuntimeValidatableTypeInfo? CreateValidatableTypeInfo(Type type, HashSet<Type> visitedTypes)
    {
        // Prevent infinite recursion by tracking visited types
        if (!visitedTypes.Add(type))
        {
            return null;
        }

        try
        {
            // Skip types that don't need validation (same logic as parameter resolver)
            if (!IsClassForType(type))
            {
                return null;
            }

            // Get validation attributes applied to the type
            var typeValidationAttributes = type
                .GetCustomAttributes<ValidationAttribute>()
                .ToArray();

            // Get all public instance properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var validatableProperties = new List<RuntimeValidatablePropertyInfo>();

            foreach (var property in properties)
            {
                // Skip properties without setters (read-only properties) for normal classes
                if (!property.CanWrite && !IsRecordType(type))
                {
                    continue;
                }

                // Skip properties marked with [FromService] or [FromKeyedService] attributes
                if (HasFromServiceAttributes(property.GetCustomAttributes()))
                {
                    continue;
                }

                // Get validation attributes for this property
                var propertyValidationAttributes = property
                    .GetCustomAttributes<ValidationAttribute>()
                    .ToArray();

                // For record types, also check constructor parameters for validation attributes
                if (propertyValidationAttributes.Length == 0 && IsRecordType(type))
                {
                    var constructorValidationAttributes = GetValidationAttributesFromConstructorParameter(type, property.Name);
                    if (constructorValidationAttributes.Length > 0)
                    {
                        propertyValidationAttributes = constructorValidationAttributes;
                    }
                }

                // Get display name
                var displayName = GetDisplayNameForProperty(property);

                // Determine if this property has a validatable type
                // Use the simpler check that doesn't cause recursion
                var hasValidatableType = IsClassForType(property.PropertyType);

                // Create the property info if it has validation attributes or a validatable type
                if (propertyValidationAttributes.Length > 0 || hasValidatableType)
                {
                    var propertyInfo = new RuntimeValidatablePropertyInfo(
                        declaringType: type,
                        propertyType: property.PropertyType,
                        name: property.Name,
                        displayName: displayName,
                        validationAttributes: propertyValidationAttributes);

                    validatableProperties.Add(propertyInfo);
                }
            }

            // Check for polymorphic derived types (JsonDerivedType attributes)
            var derivedTypes = GetDerivedTypes(type);
            foreach (var derivedType in derivedTypes)
            {
                // Recursively ensure derived types are also cached
                CreateValidatableTypeInfo(derivedType, visitedTypes);
            }

            // Only create type info if there are validation attributes on the type or validatable properties
            if (typeValidationAttributes.Length > 0 || validatableProperties.Count > 0)
            {
                return new RuntimeValidatableTypeInfo(type, validatableProperties);
            }

            return null;
        }
        finally
        {
            visitedTypes.Remove(type);
        }
    }

    private static List<Type> GetDerivedTypes(Type baseType)
    {
        var derivedTypes = new List<Type>();

        // Look for JsonDerivedType attributes on the base type
        var jsonDerivedTypeAttributes = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>();

        foreach (var attr in jsonDerivedTypeAttributes)
        {
            if (attr.DerivedType != null && attr.DerivedType.IsSubclassOf(baseType))
            {
                derivedTypes.Add(attr.DerivedType);
            }
        }

        return derivedTypes;
    }

    private static string GetDisplayNameForProperty(PropertyInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute?.Name is not null)
        {
            return displayAttribute.Name;
        }

        // For record types, also check constructor parameter for Display attribute
        if (IsRecordType(property.DeclaringType!))
        {
            var constructorDisplayName = GetDisplayNameFromConstructorParameter(property.DeclaringType!, property.Name);
            if (!string.IsNullOrEmpty(constructorDisplayName))
            {
                return constructorDisplayName;
            }
        }

        return property.Name;
    }

    private static string? GetDisplayNameFromConstructorParameter(Type type, string propertyName)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            // Find parameter that matches the property name (case-insensitive for records)
            var matchingParameter = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (matchingParameter != null)
            {
                var displayAttribute = matchingParameter.GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute?.Name is not null)
                {
                    return displayAttribute.Name;
                }
            }
        }

        return null;
    }

    private static bool IsRecordType(Type type)
    {
        // Check if the type is a record by looking for specific record-related compiler-generated methods
        // Records have a special <Clone>$ method and EqualityContract property
        return type.IsClass &&
               type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                   .Any(m => m.Name == "<Clone>$" || m.Name == "get_EqualityContract");
    }

    private static ValidationAttribute[] GetValidationAttributesFromConstructorParameter(Type type, string propertyName)
    {
        // Look for primary constructor parameters that match the property name
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // For records, prefer the primary constructor (typically the one with the most parameters)
        var primaryConstructor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (primaryConstructor != null)
        {
            var parameters = primaryConstructor.GetParameters();

            // Find parameter that matches the property name (case-insensitive for records)
            var matchingParameter = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (matchingParameter != null)
            {
                var attributes = matchingParameter.GetCustomAttributes<ValidationAttribute>().ToArray();
                if (attributes.Length > 0)
                {
                    return attributes;
                }
            }
        }

        return [];
    }

    private static Type UnwrapType(Type type)
    {
        // Handle Nullable<T>
        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            type = nullableType;
        }

        // Handle collection types - extract element type
        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>) ||
                genericDefinition == typeof(ICollection<>) ||
                genericDefinition == typeof(IList<>) ||
                genericDefinition == typeof(List<>) ||
                genericDefinition == typeof(IReadOnlyCollection<>) ||
                genericDefinition == typeof(IReadOnlyList<>))
            {
                type = type.GetGenericArguments()[0];
                return UnwrapType(type); // Recursively unwrap nested collections
            }
        }

        // Handle arrays
        if (type.IsArray)
        {
            type = type.GetElementType()!;
            return UnwrapType(type); // Recursively unwrap nested arrays
        }

        return type;
    }

    private static bool IsParsableType(Type type)
    {
        var unwrappedType = UnwrapType(type);

        // Check for built-in parsable types
        if (unwrappedType.IsPrimitive ||
            unwrappedType.IsEnum ||
            unwrappedType == typeof(string) ||
            unwrappedType == typeof(decimal) ||
            unwrappedType == typeof(DateTime) ||
            unwrappedType == typeof(DateTimeOffset) ||
            unwrappedType == typeof(TimeOnly) ||
            unwrappedType == typeof(DateOnly) ||
            unwrappedType == typeof(TimeSpan) ||
            unwrappedType == typeof(Guid) ||
            unwrappedType == typeof(Uri))
        {
            return true;
        }

        try
        {
            // Check for IParsable<T> interface
            // Check if unwrappedType implements IParsable<T> for itself
            foreach (var iface in unwrappedType.GetInterfaces())
            {
                // Look for IParsable<T> in its generic-definition form
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(IParsable<>))
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't construct the generic type, it's not parsable
        }

        try
        {
            // Check for TryParse methods
            var tryParseMethod = unwrappedType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), unwrappedType.MakeByRefType()],
                null);

            if (tryParseMethod != null && tryParseMethod.ReturnType == typeof(bool))
            {
                return true;
            }
        }
        catch
        {
            // If we can't find the method, it's not parsable
        }

        return false;
    }

    private static bool IsClassForType(Type type)
        => !IsParsableType(type) && type.IsClass;

    private static bool HasFromServiceAttributes(IEnumerable<Attribute> attributes)
    {
        // Note: Use name-based comparison for FromServices attribute defined in
        // MVC assemblies.
        return attributes.Any(attr =>
            attr.GetType().Name == "FromServicesAttribute" ||
            attr.GetType() == typeof(FromKeyedServicesAttribute));
    }

    internal sealed class RuntimeValidatablePropertyInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type declaringType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) :
            ValidatablePropertyInfo(declaringType, propertyType, name, displayName)
    {
        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    internal sealed class RuntimeValidatableTypeInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        IReadOnlyList<RuntimeValidatablePropertyInfo> members) :
            ValidatableTypeInfo(type, [.. members])
    { }
}