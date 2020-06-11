// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public static class ConversionResultProvider
    {
        public static ConversionResult ConvertTo(object value, Type typeToConvertTo)
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
                    var deserialized = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), typeToConvertTo);
                    return new ConversionResult(true, deserialized);
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
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsValueType)
            {
                // value types are only nullable if they are Nullable<T>
                return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            }
            else
            {
                // reference types are always nullable
                return true;
            }
        }
    }
}
