// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class JsonTempDataSerializer : ITempDataSerializer
{
    public IDictionary<string, object?> DeserializeData(IDictionary<string, JsonElement> data)
    {
        var dataDict = new Dictionary<string, object?>(data.Count);
        foreach (var kvp in data)
        {
            dataDict[kvp.Key] = DeserializeElement(kvp.Value);
        }
        return dataDict;
    }

    private static object? DeserializeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String => DeserializeString(element),
            JsonValueKind.Array => DeserializeArray(element),
            JsonValueKind.Object => DeserializeObject(element),
            _ => throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }

    private static object? DeserializeString(JsonElement element)
    {
        var type = GetStringType(element);
        return type switch
        {
            Type t when t == typeof(Guid) => element.GetGuid(),
            Type t when t == typeof(DateTime) => element.GetDateTime(),
            _ => element.GetString(),
        };
    }

    private static object? DeserializeArray(JsonElement element)
    {
        var length = element.GetArrayLength();
        if (length == 0)
        {
            return Array.Empty<object?>();
        }

        var array = Array.CreateInstance(GetArrayTypeInfo(element[0]), length);
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            array.SetValue(DeserializeElement(item), index++);
        }
        return array;
    }

    private static Dictionary<string, object?> DeserializeObject(JsonElement element)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = DeserializeElement(property.Value);
        }
        return dictionary;
    }

    private static Type GetArrayTypeInfo(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => typeof(bool),
            JsonValueKind.False => typeof(bool),
            JsonValueKind.Number => typeof(int),
            JsonValueKind.String => GetStringType(element),
            _ => typeof(object)
        };
    }

    private static Type GetStringType(JsonElement element)
    {
        if (element.TryGetGuid(out _))
        {
            return typeof(Guid);
        }
        if (element.TryGetDateTime(out _))
        {
            return typeof(DateTime);
        }
        return typeof(string);
    }

    public bool CanSerialize(Type type)
    {
        if (type == typeof(object) || type == typeof(object[]))
        {
            return false;
        }

        if (type.IsEnum)
        {
            return true;
        }

        if (JsonTempDataSerializerContext.Default.GetTypeInfo(type) is not null)
        {
            return true;
        }

        var dictionaryInterface = type.GetInterface(typeof(IDictionary<,>).Name);
        if (dictionaryInterface is not null)
        {
            var args = dictionaryInterface.GetGenericArguments();
            if (args[0] == typeof(string) && CanSerialize(args[1]))
            {
                return true;
            }
            return false;
        }

        var collectionInterface = type.GetInterface(typeof(ICollection<>).Name);
        if (collectionInterface is not null)
        {
            var elementType = collectionInterface.GetGenericArguments()[0];
            if (CanSerialize(elementType))
            {
                return true;
            }
            return false;
        }

        return false;
    }

    public byte[] SerializeData(IDictionary<string, object?> data)
    {
        var normalizedData = new Dictionary<string, object?>(data.Count);
        foreach (var kvp in data)
        {
            normalizedData[kvp.Key] = kvp.Value is Enum enumValue
                ? Convert.ToInt32(enumValue, CultureInfo.InvariantCulture)
                : kvp.Value;
        }
        return JsonSerializer.SerializeToUtf8Bytes<IDictionary<string, object?>>(normalizedData, JsonTempDataSerializerContext.Default.Options);
    }
}

// Simple types (non-nullable)
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
// Simple types (nullable)
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(DateTime?))]
// Collections of simple types (non-nullable)
[JsonSerializable(typeof(ICollection<int>))]
[JsonSerializable(typeof(ICollection<bool>))]
[JsonSerializable(typeof(ICollection<string>))]
[JsonSerializable(typeof(ICollection<Guid>))]
[JsonSerializable(typeof(ICollection<DateTime>))]
// Collections of simple types (nullable)
[JsonSerializable(typeof(ICollection<int?>))]
[JsonSerializable(typeof(ICollection<bool?>))]
[JsonSerializable(typeof(ICollection<Guid?>))]
[JsonSerializable(typeof(ICollection<DateTime?>))]
// Dictionaries of simple types (non-nullable)
[JsonSerializable(typeof(IDictionary<string, int>))]
[JsonSerializable(typeof(IDictionary<string, bool>))]
[JsonSerializable(typeof(IDictionary<string, string>))]
[JsonSerializable(typeof(IDictionary<string, Guid>))]
[JsonSerializable(typeof(IDictionary<string, DateTime>))]
// Dictionaries of simple types (nullable)
[JsonSerializable(typeof(IDictionary<string, int?>))]
[JsonSerializable(typeof(IDictionary<string, bool?>))]
[JsonSerializable(typeof(IDictionary<string, Guid?>))]
[JsonSerializable(typeof(IDictionary<string, DateTime?>))]
// Object arrays for nested/empty arrays
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(ICollection<object>))]
// Serialization of the TempData dictionary
[JsonSerializable(typeof(IDictionary<string, object?>))]
internal sealed partial class JsonTempDataSerializerContext : JsonSerializerContext
{
}
