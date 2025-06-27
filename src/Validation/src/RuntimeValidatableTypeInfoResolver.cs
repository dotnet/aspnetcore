#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Validation;

internal sealed class RuntimeValidatableTypeInfoResolver : IValidatableInfoResolver
{
    private static readonly ConcurrentDictionary<Type, IValidatableInfo?> _cache = new();

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

    // Parameter discovery is handled by RuntimeValidatableParameterInfoResolver
    public bool TryGetValidatableParameterInfo(
        ParameterInfo parameterInfo,
        [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
    {
        validatableInfo = null;
        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Runtime reflection is used for runtime type discovery - this is expected for the runtime resolver")]
    private static IValidatableInfo? CreateValidatableTypeInfo(Type type, HashSet<Type> visitedTypes)
    {
        // Prevent infinite recursion by tracking visited types
        if (!visitedTypes.Add(type))
        {
            return null;
        }

        try
        {
            // Skip types that don't need validation (same logic as parameter resolver)
            if (!IsClass(type))
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
                // Skip properties without setters (read-only properties)
                if (!property.CanWrite)
                {
                    continue;
                }

                // Get validation attributes for this property
                var propertyValidationAttributes = property
                    .GetCustomAttributes<ValidationAttribute>()
                    .ToArray();

                // Get display name
                var displayName = GetDisplayName(property);

                // Determine if this property has a validatable type
                var hasValidatableType = IsClass(property.PropertyType);

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

    private static string GetDisplayName(PropertyInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute?.Name is not null)
        {
            return displayAttribute.Name;
        }

        return property.Name;
    }

    private static bool IsClass(Type type)
    {
        // Skip primitives, enums, common built-in types, and types that are specially
        // handled by RDF/RDG that don't need validation if they don't have attributes
        // (Same logic as RuntimeValidatableParameterInfoResolver.IsClass)
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid))
        {
            return false;
        }

        // Check if the underlying type in a nullable is valid
        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsClass(nullableType);
        }

        return type.IsClass;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Runtime reflection is used for runtime type discovery - this is expected for the runtime resolver")]
    internal sealed class RuntimeValidatablePropertyInfo(
        Type declaringType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) :
            ValidatablePropertyInfo(declaringType, propertyType, name, displayName)
    {
        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Runtime reflection is used for runtime type discovery - this is expected for the runtime resolver")]
    internal sealed class RuntimeValidatableTypeInfo(
        Type type,
        IReadOnlyList<RuntimeValidatablePropertyInfo> members) :
            ValidatableTypeInfo(type, members.ToArray<ValidatablePropertyInfo>())
    {
    }
}