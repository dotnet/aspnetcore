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

    // Enums are stored as their Int32 value, so only enums whose underlying type always fits in an Int32 are supported.
    private static readonly HashSet<Type> _int32EnumUnderlyingTypes =
        [typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int)];

    // Compact, stable type tokens (e.g. "list[int]") are persisted instead of Type.FullName, which for generic
    // types embeds assembly version and public key token, bloating payloads and breaking across runtime upgrades.
    // _typeToName is the authoritative allow-list of directly-storable types.
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

        // object[] is the only kind stored recursively (each element carries its own type token),
        // so it is rebuilt element-by-element rather than delegated to System.Text.Json.
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

    public bool CanSerialize(Type type) => TryGetStorageType(type, out _);

    // Resolves the storage type for a runtime type, or returns false if it can't be serialized.
    // _typeToName is checked first so pre-registered kinds (scalars, nullables, arrays, List/HashSet/
    // SortedSet/Collection/ObservableCollection, Dictionary<string,T>, object[]) win. Enums fall back to
    // their Int32 (or Int32[]) form. Checking _typeToName first keeps the intentional asymmetry that
    // enum arrays are supported while enum-element collections (e.g. List<enum>) are not.
    private static bool TryGetStorageType(Type type, out Type storageType)
    {
        if (_typeToName.ContainsKey(type))
        {
            storageType = type;
            return true;
        }

        if (IsInt32Enum(type))
        {
            storageType = typeof(int);
            return true;
        }

        if (type.IsArray && IsInt32Enum(type.GetElementType()!))
        {
            storageType = typeof(int[]);
            return true;
        }

        storageType = type;
        return false;
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

    private static void WriteEntry(Utf8JsonWriter writer, object? value, Type? type)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var valueType = type ?? value.GetType();
        if (!TryGetStorageType(valueType, out var storageType))
        {
            throw new InvalidOperationException($"Cannot serialize type '{valueType}'.");
        }

        var writeValue = NormalizeEnums(value, valueType, storageType);

        writer.WriteStartObject();
        writer.WriteString("type", _typeToName[storageType]);
        writer.WritePropertyName("value");

        // object[] is the only kind stored recursively (each element carries its own type token),
        // so it is written element-by-element rather than delegated to System.Text.Json.
        if (storageType == typeof(object[]))
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
            JsonSerializer.Serialize(writer, writeValue, storageType, _options);
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

    // Enums have no direct JSON representation, so they are converted to their Int32 form to match
    // the "int"/"int[]" storage type resolved by TryGetStorageType. All other values pass through.
    private static object NormalizeEnums(object value, Type valueType, Type storageType)
    {
        if (storageType == typeof(int) && valueType.IsEnum)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        if (storageType == typeof(int[]) && valueType != typeof(int[]))
        {
            return ConvertEnumsToInts((IEnumerable)value);
        }

        return value;
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
