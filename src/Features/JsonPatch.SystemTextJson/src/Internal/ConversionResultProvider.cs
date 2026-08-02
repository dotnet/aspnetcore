// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal static class ConversionResultProvider
{
    internal static ConversionResult ConvertTo(object value, Type typeToConvertTo, JsonSerializerOptions serializerOptions)
    {
        if (value == null)
        {
            return new ConversionResult(IsNullableType(typeToConvertTo), null);
        }

        if (typeToConvertTo.IsAssignableFrom(value.GetType()))
        {
            // No need to convert
            return new ConversionResult(true, value);
        }

        return GetConvertedValue(value, serializerOptions, ref typeToConvertTo);
    }

    internal static ConversionResult CopyTo(object value, Type typeToConvertTo, JsonSerializerOptions serializerOptions)
    {
        var targetType = typeToConvertTo;
        if (value == null)
        {
            return new ConversionResult(canBeConverted: true, convertedInstance: null);
        }

        if (typeToConvertTo != value.GetType() && typeToConvertTo.IsAssignableFrom(value.GetType()))
        {
            // Keep original type
            targetType = value.GetType();
        }

        return GetConvertedValue(value, serializerOptions, ref targetType);
    }

    private static ConversionResult GetConvertedValue(object value, JsonSerializerOptions serializerOptions, ref Type targetType)
    {
        // Workaround for the https://github.com/dotnet/runtime/issues/113926
        if (targetType.Name == "JsonValuePrimitive`1")
        {
            targetType = typeof(JsonElement);
        }

        try
        {
            var deserialized = ConvertToTargetType(value, targetType, serializerOptions);
            return new ConversionResult(true, deserialized);
        }
        catch
        {
            return new ConversionResult(canBeConverted: false, convertedInstance: null);
        }
    }

    private static bool IsNullableType(Type type)
    {
        if (type.IsValueType)
        {
            // value types are only nullable if they are Nullable<T>
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        // reference types are always nullable
        return true;
    }

    private static object ConvertToTargetType(object value, Type targetType, JsonSerializerOptions serializerOptions)
    {
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement, targetType, serializerOptions);
        }

        using JsonDocument doc = JsonSerializer.SerializeToDocument(value, serializerOptions);
        return JsonSerializer.Deserialize(doc.RootElement, targetType, serializerOptions);
    }
}
