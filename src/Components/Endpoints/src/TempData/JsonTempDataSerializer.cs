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
        return map;
    }

    public IDictionary<string, (object? Value, Type? Type)> DeserializeData(IDictionary<string, JsonElement> data)
    {
        var result = new Dictionary<string, (object? Value, Type? Type)>(data.Count);

        foreach (var (key, element) in data)
        {
            if (element.ValueKind is JsonValueKind.Null)
            {
                result[key] = (null, null);
                continue;
            }

            var typeName = element.GetProperty("type").GetString()!;
            var valueElement = element.GetProperty("value");

            if (!_nameToType.TryGetValue(typeName, out var type))
            {
                throw new InvalidOperationException($"Cannot deserialize type '{typeName}'.");
            }

            var value = JsonSerializer.Deserialize(valueElement, type);
            result[key] = (value, type);
        }
        return result;
    }

    public bool CanSerialize(Type type)
    {
        if (_supportedTypes.Contains(type) || type.IsEnum)
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

            if (value is null)
            {
                writer.WriteNullValue();
                continue;
            }

            if (type is null || !CanSerialize(type))
            {
                throw new InvalidOperationException($"Cannot serialize type '{type}'.");
            }

            var collectionElementType = GetCollectionElementType(type);

            object writeValue = type.IsEnum ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : collectionElementType is not null && collectionElementType.IsEnum ? ConvertEnumsToInts((IEnumerable)value)
                : value;

            var writeType = type.IsEnum ? typeof(int)
                : collectionElementType?.IsEnum == true ? typeof(int[])
                : collectionElementType is not null && !type.IsArray ? collectionElementType.MakeArrayType()
                : type;

            writer.WriteStartObject();
            writer.WriteString("type", writeType.FullName);
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, writeValue, writeValue.GetType());
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.ToArray();
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

    private static List<int> ConvertEnumsToInts(IEnumerable values)
    {
        var result = new List<int>();
        foreach (var item in values)
        {
            result.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
        }
        return result;
    }
}
