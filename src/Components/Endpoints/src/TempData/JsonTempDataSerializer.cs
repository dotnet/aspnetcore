// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class JsonTempDataSerializer : ITempDataSerializer
{
    private static readonly Dictionary<JsonValueKind, Func<JsonElement, object?>> _elementConverters = new(4)
    {
        { JsonValueKind.Number, static e => e.GetInt32() },
        { JsonValueKind.True, static e => e.GetBoolean() },
        { JsonValueKind.False, static e => e.GetBoolean() },
        { JsonValueKind.Null, static _ => null },
    };

    private static Type GetArrayTypeInfo(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.String => typeof(string),
            JsonValueKind.Number => typeof(int),
            _ => typeof(object)
        };
    }

    public object? Deserialize(JsonElement element)
    {
        try
        {
            return DeserializeSimpleType(element);
        }
        catch (InvalidOperationException)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Array => DeserializeArray(element),
                JsonValueKind.Object => DeserializeDictionaryEntry(element),
                _ => throw new InvalidOperationException($"TempData cannot deserialize value of type '{element.ValueKind}'.")
            };
        }
    }

    private static object? DeserializeSimpleType(JsonElement element)
    {
        if (_elementConverters.TryGetValue(element.ValueKind, out var converter))
        {
            return converter(element);
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => DeserializeString(element),
            _ => throw new InvalidOperationException($"TempData cannot deserialize value of type '{element.ValueKind}'.")
        };
    }

    private static object? DeserializeString(JsonElement element)
    {
        if (element.TryGetGuid(out var guid))
        {
            return guid;
        }
        if (element.TryGetDateTime(out var dateTime))
        {
            return dateTime;
        }
        return element.GetString();
    }

    private static object? DeserializeArray(JsonElement arrayElement)
    {
        var arrayLength = arrayElement.GetArrayLength();
        if (arrayLength == 0)
        {
            return null;
        }
        var array = Array.CreateInstance(GetArrayTypeInfo(arrayElement[0].ValueKind), arrayLength);
        for (var i = 0; i < arrayLength; i++)
        {
            array.SetValue(DeserializeSimpleType(arrayElement[i]), i);
        }
        return array;
    }

    private static Dictionary<string, object?> DeserializeDictionaryEntry(JsonElement objectElement)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in objectElement.EnumerateObject())
        {
            // JSON object keys are always strings by specification
            dictionary[item.Name] = DeserializeSimpleType(item.Value);
        }

        return dictionary;
    }

    public bool EnsureObjectCanBeSerialized(Type type)
    {
        var actualType = type;
        if (type.IsArray)
        {
            actualType = type.GetElementType();
        }
        else if (type.IsGenericType)
        {
            var genericTypeArguments = type.GenericTypeArguments;
            if (ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IList<>)) != null && genericTypeArguments.Length == 1)
            {
                actualType = genericTypeArguments[0];
            }
            else if (ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IDictionary<,>)) != null && genericTypeArguments.Length == 2 && genericTypeArguments[0] == typeof(string))
            {
                actualType = genericTypeArguments[1];
            }
            else
            {
                return false;
            }
        }
        if (actualType is null)
        {
            return false;
        }

        actualType = Nullable.GetUnderlyingType(actualType) ?? actualType;

        if (!IsSimpleType(actualType))
        {
            return false;
        }
        return true;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
            type.IsEnum ||
            type.Equals(typeof(decimal)) ||
            type.Equals(typeof(string)) ||
            type.Equals(typeof(DateTime)) ||
            type.Equals(typeof(Guid)) ||
            type.Equals(typeof(DateTimeOffset)) ||
            type.Equals(typeof(TimeSpan)) ||
            type.Equals(typeof(Uri));
    }
}
