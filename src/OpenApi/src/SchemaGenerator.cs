// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal static class SchemaGenerator
{
    internal static string GetOpenApiSchemaType(Type? inputType)
    {
        if (inputType == null)
        {
            throw new ArgumentNullException(nameof(inputType));
        }

        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;

        if (typeof(string).IsAssignableFrom(type) || typeof(DateTime).IsAssignableTo(type))
        {
            return "string";
        }
        else if (typeof(bool).IsAssignableFrom(type))
        {
            return "boolean";
        }
        else if (typeof(int).IsAssignableFrom(type)
            || typeof(double).IsAssignableFrom(type)
            || typeof(float).IsAssignableFrom(type))
        {
            return "number";
        }
        else if (typeof(long).IsAssignableFrom(type))
        {
            return "integer";
        }
        else if (type.IsArray)
        {
            return "array";
        }
        else
        {
            return "object";
        }
    }
}
