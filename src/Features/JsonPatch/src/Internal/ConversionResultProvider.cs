// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public static class ConversionResultProvider
{
    public static ConversionResult ConvertTo(object value, Type typeToConvertTo)
    {
        return ConvertTo(value, typeToConvertTo, null);
    }

    internal static ConversionResult ConvertTo(object value, Type typeToConvertTo, IContractResolver contractResolver)
    {
        if (value == null)
        {
            return new ConversionResult(IsNullableType(typeToConvertTo), null);
        }
        else if (typeToConvertTo.IsAssignableFrom(value.GetType()))
        {
            // No need to convert
            return new ConversionResult(true, value);
        }
        else
        {
            try
            {
                if (contractResolver == null)
                {
                    var deserialized = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), typeToConvertTo);
                    return new ConversionResult(true, deserialized);
                }
                else
                {
                    var serializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = contractResolver
                    };
                    var deserialized = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), typeToConvertTo, serializerSettings);
                    return new ConversionResult(true, deserialized);
                }
            }
            catch
            {
                return new ConversionResult(canBeConverted: false, convertedInstance: null);
            }
        }
    }

    public static ConversionResult CopyTo(object value, Type typeToConvertTo)
    {
        var targetType = typeToConvertTo;
        if (value == null)
        {
            return new ConversionResult(canBeConverted: true, convertedInstance: null);
        }
        else if (typeToConvertTo.IsAssignableFrom(value.GetType()))
        {
            // Keep original type
            targetType = value.GetType();
        }
        try
        {
            var deserialized = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), targetType);
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
        else
        {
            // reference types are always nullable
            return true;
        }
    }
}
