// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class JsonStoredDataSerializer : IStoredDataSerializer
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    private static readonly Type ObjectArrayType = typeof(object[]);

    private static readonly HashSet<Type> _supportedCollectionDefinitions =
    [
        typeof(List<>),
        typeof(HashSet<>),
        typeof(System.Collections.ObjectModel.Collection<>),
    ];

    public bool CanSerialize(Type type)
    {
        if (type == ObjectArrayType)
        {
            return true;
        }

        if (IsSupportedElement(type))
        {
            return true;
        }

        if (type.IsSZArray)
        {
            return IsSupportedElement(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();

            if (definition == typeof(Dictionary<,>))
            {
                return arguments[0] == typeof(string) && IsSupportedElement(arguments[1]);
            }

            if (_supportedCollectionDefinitions.Contains(definition))
            {
                return IsSupportedElement(arguments[0]);
            }
        }

        return false;
    }

    private static bool IsSupportedElement(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(int)
            || type == typeof(bool)
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || (type.IsEnum && type.GetEnumUnderlyingType() == typeof(int));
    }

    public IDictionary<string, object?> DeserializeData(IDictionary<string, JsonElement> data)
    {
        var result = new Dictionary<string, object?>(data.Count);

        foreach (var (key, element) in data)
        {
            result[key] = DeserializeEntry(element);
        }
        return result;
    }

    private object? DeserializeEntry(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        var typeName = element.GetProperty("type").GetString()!;
        var valueElement = element.GetProperty("value");
        var type = ResolveType(typeName);

        if (type == ObjectArrayType)
        {
            var array = new object?[valueElement.GetArrayLength()];
            var index = 0;
            foreach (var item in valueElement.EnumerateArray())
            {
                array[index++] = DeserializeEntry(item);
            }
            return array;
        }

        return JsonSerializer.Deserialize(valueElement, type, _options);
    }

    [return: DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)]
    private Type ResolveType(string typeName)
    {
        var type = Type.GetType(typeName, throwOnError: false);
        if (type is null || !CanSerialize(type))
        {
            throw new InvalidOperationException($"Cannot deserialize type '{typeName}'.");
        }

        return type;
    }

    private static string GetStorageToken(Type type)
    {
        if (type.IsEnum)
        {
            return "System.Int32";
        }

        if (type.IsSZArray)
        {
            return GetStorageToken(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericType)
        {
            var arguments = Array.ConvertAll(type.GetGenericArguments(), GetStorageToken);
            return $"{type.GetGenericTypeDefinition().FullName}[{string.Join(',', arguments)}]";
        }

        return type.ToString();
    }

    public byte[] SerializeData(IDictionary<string, object?> data)
    {
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var (key, value) in data)
        {
            writer.WritePropertyName(key);
            WriteEntry(writer, value, value?.GetType());
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

        writer.WriteStartObject();
        writer.WriteString("type", GetStorageToken(valueType));
        writer.WritePropertyName("value");

        if (valueType == ObjectArrayType)
        {
            writer.WriteStartArray();
            foreach (var item in (object?[])value)
            {
                WriteEntry(writer, item, item?.GetType());
            }
            writer.WriteEndArray();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, valueType, _options);
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

    public object? DeserializeValue(ReadOnlySpan<byte> utf8Json, [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] Type targetType)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(utf8Json, _options);
        if (element.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (targetType != typeof(object) && CanSerialize(targetType))
        {
            var valueElement = element.GetProperty("value");
            return JsonSerializer.Deserialize(valueElement, targetType, _options);
        }

        return DeserializeEntry(element);
    }
}
