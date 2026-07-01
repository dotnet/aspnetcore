// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class JsonTempDataAndSessionSerializer : ITempDataAndSessionSerializer
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<Type, string> _scalarNames = new()
    {
        [typeof(int)] = "int",
        [typeof(bool)] = "bool",
        [typeof(string)] = "string",
        [typeof(Guid)] = "guid",
        [typeof(DateTime)] = "datetime",
    };

    private static readonly HashSet<Type> _supportedTypes = [.. _scalarNames.Keys];

    private static readonly HashSet<Type> _int32EnumUnderlyingTypes =
        [typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int)];

    private static readonly Dictionary<string, Type> _nameToType = BuildNameToType();
    private static readonly Dictionary<Type, string> _typeToName = BuildTypeToName(_nameToType);

    private static Dictionary<string, Type> BuildNameToType()
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var (scalar, name) in _scalarNames)
        {
            map[name] = scalar;
            AddCollectionTypeNames(map, scalar, name);

            if (scalar.IsValueType)
            {
                var nullable = typeof(Nullable<>).MakeGenericType(scalar);
                map[$"{name}?"] = nullable;
                AddCollectionTypeNames(map, nullable, $"{name}?");
            }
        }

        map["object[]"] = typeof(object[]);
        return map;
    }

    private static void AddCollectionTypeNames(Dictionary<string, Type> map, Type element, string name)
    {
        map[$"{name}[]"] = element.MakeArrayType();
        map[$"list[{name}]"] = typeof(List<>).MakeGenericType(element);
        map[$"set[{name}]"] = typeof(HashSet<>).MakeGenericType(element);
        map[$"sortedset[{name}]"] = typeof(SortedSet<>).MakeGenericType(element);
        map[$"collection[{name}]"] = typeof(Collection<>).MakeGenericType(element);
        map[$"observable[{name}]"] = typeof(ObservableCollection<>).MakeGenericType(element);
        map[$"dict[{name}]"] = typeof(Dictionary<,>).MakeGenericType(typeof(string), element);
    }

    private static Dictionary<Type, string> BuildTypeToName(Dictionary<string, Type> nameToType)
    {
        var map = new Dictionary<Type, string>();
        foreach (var (name, type) in nameToType)
        {
            map[type] = name;
        }
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

        var value = JsonSerializer.Deserialize(valueElement, type, _options);
        return (value, type);
    }

    public bool CanSerialize(Type type)
    {
        if (IsSupportedElement(type))
        {
            return true;
        }

        if (type == typeof(object[]))
        {
            return true;
        }

        if (type.IsArray && IsSupportedElement(type.GetElementType()!))
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
            && type.GetGenericArguments()[0] == typeof(string) && IsSupportedElement(type.GetGenericArguments()[1]))
        {
            return true;
        }

        var collectionElementType = GetCollectionElementType(type);
        if (collectionElementType is not null)
        {
            return _typeToName.ContainsKey(type);
        }

        return false;
    }

    private static bool IsSupportedElement(Type type)
    {
        if (_supportedTypes.Contains(type) || IsInt32Enum(type))
        {
            return true;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType is not null && _supportedTypes.Contains(underlyingType);
    }

    private static bool IsInt32Enum(Type type)
        => type.IsEnum && _int32EnumUnderlyingTypes.Contains(type.GetEnumUnderlyingType());

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

        if (!_typeToName.TryGetValue(writeType, out var writeTypeName))
        {
            throw new InvalidOperationException($"Cannot serialize type '{writeType}'.");
        }

        writer.WriteStartObject();
        writer.WriteString("type", writeTypeName);
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
            JsonSerializer.Serialize(writer, writeValue, writeType, _options);
        }

        writer.WriteEndObject();
    }

    public byte[] SerializeValue(object value, Type type)
    {
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);

        WriteEntry(writer, value, type);
        writer.Flush();

        return buffer.ToArray();
    }

    public (object? Value, Type? Type) DeserializeValue(ReadOnlySpan<byte> utf8Json)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(utf8Json, _options);
        return DeserializeEntry(element);
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

        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                return @interface.GetGenericArguments()[0];
            }
        }

        return null;
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
}
