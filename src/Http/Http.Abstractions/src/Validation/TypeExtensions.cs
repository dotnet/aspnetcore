// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether the specified type is an enumerable type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type is enumerable; otherwise, <see langword="false"/>.</returns>
    public static bool IsEnumerable(this Type type)
    {
        // Check if type itself is an IEnumerable
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(List<>) ||
            type.GetGenericTypeDefinition() == typeof(IList<>)))
        {
            return true;
        }

        // Or an array
        if (type.IsArray)
        {
            return true;
        }

        // Then evaluate if it implements IEnumerable and is not a string
        if (typeof(IEnumerable).IsAssignableFrom(type) &&
            type != typeof(string))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified type is a nullable type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type is nullable; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullable(this Type type)
    {
        if (type.IsValueType)
        {
            return false;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to get the <see cref="RequiredAttribute"/> from the specified array of validation attributes.
    /// </summary>
    /// <param name="attributes">The array of <see cref="ValidationAttribute"/> to search.</param>
    /// <param name="requiredAttribute">The found <see cref="RequiredAttribute"/> if available, otherwise null.</param>
    /// <returns><see langword="true"/> if a <see cref="RequiredAttribute"/> is found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetRequiredAttribute(this ValidationAttribute[] attributes, [NotNullWhen(true)] out RequiredAttribute? requiredAttribute)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is RequiredAttribute requiredAttr)
            {
                requiredAttribute = requiredAttr;
                return true;
            }
        }

        requiredAttribute = null;
        return false;
    }

    /// <summary>
    /// Gets all types that the specified type implements or inherits from.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>A collection containing all implemented interfaces and all base types of the given type.</returns>
    public static List<Type> GetAllImplementedTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var implementedTypes = new List<Type>();

        // Yield all interfaces directly and indirectly implemented by this type
        foreach (var interfaceType in type.GetInterfaces())
        {
            implementedTypes.Add(interfaceType);
        }

        // Finally, walk up the inheritance chain
        var baseType = type.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            implementedTypes.Add(baseType);
            baseType = baseType.BaseType;
        }

        return implementedTypes;
    }

    /// <summary>
    /// Determines whether the specified type implements the given interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>True if the type implements the specified interface; otherwise, false.</returns>
    public static bool ImplementsInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(interfaceType);

        // Check if interfaceType is actually an interface
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"Type {interfaceType.FullName} is not an interface.", nameof(interfaceType));
        }

        return interfaceType.IsAssignableFrom(type);
    }
}
