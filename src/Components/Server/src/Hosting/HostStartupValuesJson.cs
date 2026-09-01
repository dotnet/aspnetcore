// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Hosting;

internal static class HostStartupValuesJson
{
    internal static bool TryDeserialize(string? json, out Dictionary<string, string> values)
    {
        values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (json is null)
        {
            return false;
        }

        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    return false;
                }

                var key = reader.GetString()!;
                if (!reader.Read() || reader.TokenType is not JsonTokenType.String)
                {
                    return false;
                }

                if (!values.TryAdd(key, reader.GetString()!))
                {
                    return false;
                }
            }

            return reader.TokenType is JsonTokenType.EndObject && !reader.Read();
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
