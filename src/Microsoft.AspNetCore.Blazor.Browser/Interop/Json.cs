// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    /// <summary>
    /// Temporary internal JSON serialization library.
    /// This will be removed when public JSON methods are added to Microsoft.AspNetCore.Blazor
    /// (which will be needed for HTTP support at least).
    /// </summary>
    internal static class Json
    {
        public static string Serialize(object value)
        {
            return MiniJSON.Json.Serialize(value);
        }

        public static T Deserialize<T>(string json)
        {
            var deserialized = MiniJSON.Json.Deserialize(json);
            return (T)CoerceShallow(deserialized, typeof(T));
        }

        private static object CoerceShallow(object deserializedValue, Type typeOfT)
        {
            if (typeOfT == typeof(object)) { return deserializedValue; }

            if (deserializedValue == null)
            {
                // Return default value for type
                if (typeOfT.GetTypeInfo().IsValueType)
                {
                    return Activator.CreateInstance(typeOfT);
                }
                else
                {
                    return null;
                }
            }
            else if (deserializedValue is int || deserializedValue is long)
            {
                var deserializedValueLong = (long)deserializedValue;
                if (typeOfT == typeof(int)) { return (int)deserializedValueLong; }
                if (typeOfT == typeof(int?)) { return new int?((int)deserializedValueLong); }
                if (typeOfT == typeof(uint)) { return (uint)deserializedValueLong; }
                if (typeOfT == typeof(long)) { return (long)deserializedValueLong; }
                if (typeOfT == typeof(ulong)) { return (ulong)deserializedValueLong; }
                if (typeOfT == typeof(short)) { return (short)deserializedValueLong; }
                if (typeOfT == typeof(ushort)) { return (ushort)deserializedValueLong; }
                if (typeOfT == typeof(float)) { return (float)deserializedValueLong; }
                if (typeOfT == typeof(double)) { return (double)deserializedValueLong; }

                throw new ArgumentException($"Can't convert JSON value parsed as type {deserializedValue.GetType().FullName} to a value of type {typeOfT.FullName}");
            }
            else if (deserializedValue is string s)
            {
                if (typeOfT == typeof(string))
                {
                    return deserializedValue;
                }

                if (typeOfT == typeof(DateTime))
                {
                    return DateTime.Parse(s);
                }

                if (typeOfT == typeof(DateTime?))
                {
                    return new DateTime?(DateTime.Parse(s));
                }

                throw new ArgumentException($"Can't convert JSON value parsed as type {deserializedValue.GetType().FullName} to a value of type {typeOfT.FullName}");
            }
            else if (deserializedValue is bool)
            {
                if (typeOfT == typeof(bool))
                {
                    return deserializedValue;
                }

                throw new ArgumentException($"Can't convert JSON value parsed as type {deserializedValue.GetType().FullName} to a value of type {typeOfT.FullName}");
            }
            else if (deserializedValue is double)
            {
                var deserializedValueDouble = (double)deserializedValue;
                if (typeOfT == typeof(float)) { return (float)deserializedValueDouble; }
                if (typeOfT == typeof(double)) { return deserializedValueDouble; }

                throw new ArgumentException($"Can't convert JSON value parsed as type {deserializedValue.GetType().FullName} to a value of type {typeOfT.FullName}");
            }
            else if (deserializedValue is List<object>)
            {
                if (!typeOfT.IsArray)
                {
                    return null;
                    //throw new ArgumentException($"Can't convert JSON array to type {typeOfT.FullName}, because that's not an array type.");
                }

                var deserializedValueList = (List<object>)deserializedValue;
                var count = deserializedValueList.Count;
                var elementType = typeOfT.GetElementType();
                var result = Array.CreateInstance(elementType, count);
                for (var index = 0; index < count; index++)
                {
                    var deserializedPropertyValue = deserializedValueList[index];
                    var mappedPropertyValue = CoerceShallow(deserializedPropertyValue, elementType);
                    result.SetValue(mappedPropertyValue, index);
                }
                return result;
            }
            else if (deserializedValue is Dictionary<string, object>)
            {
                var result = Activator.CreateInstance(typeOfT);
                var deserializedPropertyDict = (Dictionary<string, object>)deserializedValue;
                foreach (var propInfo in typeOfT.GetRuntimeProperties())
                {
                    if (deserializedPropertyDict.TryGetValue(propInfo.Name, out var deserializedPropertyValue))
                    {
                        var setMethod = propInfo.SetMethod;
                        if (!object.Equals(setMethod, null))
                        {
                            var mappedPropertyValue = CoerceShallow(deserializedPropertyValue, propInfo.PropertyType);
                            setMethod.Invoke(result, new[] { mappedPropertyValue });
                        }
                    }
                }

                return result;
            }
            else
            {
                throw new ArgumentException($"Unexpected type received by CoerceShallow. Type was: { deserializedValue.GetType().FullName }");
            }
        }
    }
}
