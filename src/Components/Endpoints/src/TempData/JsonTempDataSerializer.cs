// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class JsonTempDataSerializer : ITempDataSerializer
{
    private static readonly HashSet<Type> _supportedTypes = [typeof(int), typeof(bool), typeof(string), typeof(Guid), typeof(DateTime)];

    private static readonly Dictionary<string, Type> _nameToType = BuildNameToType();

    private static Dictionary<string, Type> BuildNameToType()
    {
        var map = new Dictionary<string, Type>();
        foreach (var type in _supportedTypes)
        {
            map[type.FullName!] = type;
            map[type.MakeArrayType().FullName!] = type.MakeArrayType();
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
            map[dictType.FullName!] = dictType;
        }
        map[typeof(object[]).FullName!] = typeof(object[]);
        return map;
    }

    public IDictionary<string, (object? Value, Type? Type)> DeserializeData(IDictionary<string, JsonElement> data)
    {
        var result = new Dictionary<string, (object? Value, Type? Type)>(data.Count);

        foreach (var (key, element) in data)
        {
            result[key] = DeserializeEntry(element);
        }
        return result;
    }

    private static (object? Value, Type? Type) DeserializeEntry(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null)
        {
            return (null, null);
        }

        var typeName = element.GetProperty("type").GetString()!;
        var valueElement = element.GetProperty("value");

        if (!_nameToType.TryGetValue(typeName, out var type))
        {
            throw new InvalidOperationException($"Cannot deserialize type '{typeName}'.");
        }

        if (type == typeof(object[]))
        {
            var array = new object?[valueElement.GetArrayLength()];
            var index = 0;
            foreach (var item in valueElement.EnumerateArray())
            {
                array[index++] = DeserializeEntry(item).Value;
            }
            return (array, type);
        }

        var value = JsonSerializer.Deserialize(valueElement, type);
        return (value, type);
    }

    public bool CanSerialize(Type type)
    {
        if (_supportedTypes.Contains(type) || type.IsEnum)
        {
            return true;
        }

        if (type == typeof(object[]))
        {
            return true;
        }

        if (type.IsArray && (_supportedTypes.Contains(type.GetElementType()!) || type.GetElementType()!.IsEnum))
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) && type.GetGenericArguments()[0] == typeof(string) && _supportedTypes.Contains(type.GetGenericArguments()[1]))
        {
            return true;
        }

        var collectionElementType = GetCollectionElementType(type);
        if (collectionElementType is not null)
        {
            return _supportedTypes.Contains(collectionElementType) || collectionElementType.IsEnum;
        }

        return false;
    }

    public byte[] SerializeData(IDictionary<string, (object? Value, Type? Type)> data)
    {
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var (key, (value, type)) in data)
        {
            writer.WritePropertyName(key);
            WriteEntry(writer, value, type);
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.ToArray();
    }

    private void WriteEntry(Utf8JsonWriter writer, object? value, Type? type)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var valueType = type ?? value.GetType();
        if (!CanSerialize(valueType))
        {
            throw new InvalidOperationException($"Cannot serialize type '{valueType}'.");
        }

        var collectionElementType = GetCollectionElementType(valueType);
        var (writeValue, writeType) = NormalizeValue(value, valueType, collectionElementType);

        writer.WriteStartObject();
        writer.WriteString("type", writeType.FullName);
        writer.WritePropertyName("value");

        if (writeType == typeof(object[]))
        {
            writer.WriteStartArray();
            foreach (var item in (object?[])writeValue)
            {
                WriteEntry(writer, item, item?.GetType());
            }
            writer.WriteEndArray();
        }
        else
        {
            JsonSerializer.Serialize(writer, writeValue, writeType);
        }

        writer.WriteEndObject();
    }

    private static (object Value, Type Type) NormalizeValue(object value, Type type, Type? collectionElementType)
    {
        if (type.IsEnum)
        {
            return (Convert.ToInt32(value, CultureInfo.InvariantCulture), typeof(int));
        }

        if (collectionElementType?.IsEnum == true)
        {
            return (ConvertEnumsToInts((IEnumerable)value), typeof(int[]));
        }

        if (collectionElementType is not null && !type.IsArray)
        {
            return (ConvertToArray((IEnumerable)value, collectionElementType), collectionElementType.MakeArrayType());
        }

        return (value, type);
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return null;
        }

        var collectionInterface = type.GetInterface(typeof(ICollection<>).Name);
        return collectionInterface?.GetGenericArguments()[0];
    }

    private static int[] ConvertEnumsToInts(IEnumerable values)
    {
        var result = new List<int>();
        foreach (var item in values)
        {
            result.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
        }
        return result.ToArray();
    }

    private static Array ConvertToArray(IEnumerable values, Type elementType)
    {
        var list = new ArrayList();
        foreach (var item in values)
        {
            list.Add(item);
        }
        return list.ToArray(elementType);
    }
}
