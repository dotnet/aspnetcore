// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class TypeExtensions
{
    public static bool IsEnumerable(this Type type)
    {
        // Check if type itself is an IEnumerable
        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(List<>)))
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
}
