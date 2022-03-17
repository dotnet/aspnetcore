// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class ChangeEventArgsReader
{
    private static readonly JsonEncodedText ValueKey = JsonEncodedText.Encode("value");

    internal static ChangeEventArgs Read(JsonElement jsonElement)
    {
        var changeArgs = new ChangeEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(ValueKey.EncodedUtf8Bytes))
            {
                var value = property.Value;
                switch (value.ValueKind)
                {
                    case JsonValueKind.Null:
                        break;
                    case JsonValueKind.String:
                        changeArgs.Value = value.GetString();
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        changeArgs.Value = value.GetBoolean();
                        break;
                    case JsonValueKind.Array:
                        changeArgs.Value = GetJsonElementStringArrayValue(value);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported {nameof(ChangeEventArgs)} value {jsonElement}.");
                }
                return changeArgs;
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return changeArgs;
    }

    private static string?[] GetJsonElementStringArrayValue(JsonElement jsonElement)
    {
        var result = new string?[jsonElement.GetArrayLength()];
        var elementIndex = 0;

        foreach (var arrayElement in jsonElement.EnumerateArray())
        {
            if (arrayElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    $"Unsupported {nameof(JsonElement)} value kind '{arrayElement.ValueKind}' " +
                    $"(expected '{JsonValueKind.String}').");
            }

            result[elementIndex] = arrayElement.GetString();
            elementIndex++;
        }

        return result;
    }
}
