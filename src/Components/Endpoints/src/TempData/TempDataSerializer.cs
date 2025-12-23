// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class TempDataSerializer
{
    public static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                if (element.TryGetGuid(out var guid))
                {
                    return guid;
                }
                if (element.TryGetDateTime(out var dateTime))
                {
                    return dateTime;
                }
                return element.GetString();
            case JsonValueKind.Number:
                return element.GetInt32();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                return DeserializeArray(element);
            case JsonValueKind.Object:
                return DeserializeDictionaryEntry(element);
            default:
                throw new InvalidOperationException($"TempData cannot deserialize value of type '{element.ValueKind}'.");
        }
    }

    public static object? DeserializeArray(JsonElement arrayElement)
    {
        var arrayLength = arrayElement.GetArrayLength();
        if (arrayLength == 0)
        {
            return null;
        }
        if (arrayElement[0].ValueKind == JsonValueKind.String)
        {
            var array = new List<string?>(arrayLength);
            foreach (var item in arrayElement.EnumerateArray())
            {
                array.Add(item.GetString());
            }
            return array.ToArray();
        }
        else if (arrayElement[0].ValueKind == JsonValueKind.Number)
        {
            var array = new List<int>(arrayLength);
            foreach (var item in arrayElement.EnumerateArray())
            {
                array.Add(item.GetInt32());
            }
            return array.ToArray();
        }
        throw new InvalidOperationException($"TempData cannot deserialize array of type '{arrayElement[0].ValueKind}'.");
    }

    private static Dictionary<string, string?> DeserializeDictionaryEntry(JsonElement objectElement)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var item in objectElement.EnumerateObject())
        {
            dictionary[item.Name] = item.Value.GetString();
        }
        return dictionary;
    }

    public static bool CanSerializeType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return
            type.IsEnum ||
            type == typeof(int) ||
            type == typeof(string) ||
            type == typeof(bool) ||
            type == typeof(DateTime) ||
            type == typeof(Guid) ||
            typeof(ICollection<int>).IsAssignableFrom(type) ||
            typeof(ICollection<string>).IsAssignableFrom(type) ||
            typeof(IDictionary<string, string>).IsAssignableFrom(type);
    }
}
