#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Validation;

[RequiresUnreferencedCode("Uses unbounded Reflection to inspect property types.")]
internal sealed class RuntimeValidatableTypeInfoResolver : IValidatableInfoResolver
{
    private static readonly ConcurrentDictionary<Type, IValidatableInfo?> _cache = new();

    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? info)
    {
        info = _cache.GetOrAdd(type, static type => BuildValidatableTypeInfo(type, new HashSet<Type>()));
        return info is not null;
    }

    // Parameter discovery is handled elsewhere
    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? info)
    {
        info = null;
        return false;
    }

    private static IValidatableInfo? BuildValidatableTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] Type type, HashSet<Type> visitedTypes)
    {
        // Prevent cycles - if we've already seen this type, return null
        if (!visitedTypes.Add(type))
        {
            return null;
        }

        try
        {
            // Bail out early if this isn't a validatable class
            if (!IsValidatableClass(type))
            {
                return null;
            }

            // Get public instance properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            var validatableProperties = new List<RuntimeValidatablePropertyInfo>();

            foreach (var property in properties)
            {
                var propertyInfo = BuildValidatablePropertyInfo(property, visitedTypes);
                if (propertyInfo is not null)
                {
                    validatableProperties.Add(propertyInfo);
                }
            }

            // Only create type info if there are validatable properties
            if (validatableProperties.Count > 0)
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

    private static RuntimeValidatablePropertyInfo? BuildValidatablePropertyInfo(PropertyInfo property, HashSet<Type> visitedTypes)
    {
        var validationAttributes = property
            .GetCustomAttributes<ValidationAttribute>()
            .ToArray();

        // Check if the property type itself is validatable (recursive check)
        var hasValidatableType = false;
        if (IsValidatableClass(property.PropertyType))
        {
            var nestedTypeInfo = BuildValidatableTypeInfo(property.PropertyType, visitedTypes);
            hasValidatableType = nestedTypeInfo is not null;
        }

        // Only create property info if it has validation attributes or a validatable type
        if (validationAttributes.Length > 0 || hasValidatableType)
        {
            var displayName = GetDisplayName(property);
            return new RuntimeValidatablePropertyInfo(
                property.DeclaringType!,
                property.PropertyType,
                property.Name,
                displayName,
                validationAttributes);
        }

        return null;
    }

    private static string GetDisplayName(PropertyInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute is not null)
        {
            return displayAttribute.Name ?? property.Name;
        }

        return property.Name;
    }

    private static bool IsValidatableClass(Type type)
    {
        // Skip primitives, enums, common built-in types, and types that are specially
        // handled by RDF/RDG that don't need validation if they don't have attributes
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid) ||
            type == typeof(IFormFile) ||
            type == typeof(IFormFileCollection) ||
            type == typeof(IFormCollection) ||
            type == typeof(HttpContext) ||
            type == typeof(HttpRequest) ||
            type == typeof(HttpResponse) ||
            type == typeof(ClaimsPrincipal) ||
            type == typeof(CancellationToken) ||
            type == typeof(Stream) ||
            type == typeof(PipeReader))
        {
            return false;
        }

        // Check if the underlying type in a nullable is valid
        if (Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsValidatableClass(nullableType);
        }

        return type.IsClass || type.IsValueType;
    }

    internal sealed class RuntimeValidatableTypeInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        IReadOnlyList<RuntimeValidatablePropertyInfo> members) :
            ValidatableTypeInfo(type, members.Cast<ValidatablePropertyInfo>().ToArray())
    {
    }

    internal sealed class RuntimeValidatablePropertyInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type declaringType,
        Type propertyType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes) :
            ValidatablePropertyInfo(declaringType, propertyType, name, displayName)
    {
        private readonly ValidationAttribute[] _validationAttributes = validationAttributes;

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }
}