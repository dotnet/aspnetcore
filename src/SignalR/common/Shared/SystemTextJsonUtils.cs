// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Internal
{
    internal static class SystemTextJsonUtils
    {
        internal static bool CheckRead(ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidDataException("Unexpected end when reading JSON.");
            }

            return true;
        }

        internal static void EnsureObjectStart(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{GetTokenString(reader.TokenType)}'. Expected a JSON Object.");
            }
        }

        internal static string GetTokenString(JsonTokenType tokenType)
        {
            switch (tokenType)
            {
                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    return "StartObject";
                case JsonTokenType.StartArray:
                    return "Array";
                case JsonTokenType.PropertyName:
                    return "PropertyName";
                default:
                    break;
            }
            return tokenType.ToString();
        }

        internal static void EnsureArrayStart(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{GetTokenString(reader.TokenType)}'. Expected a JSON Array.");
            }
        }

        // Remove after https://github.com/dotnet/corefx/issues/33295 is done
        internal static void Skip(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
            {
                int depth = reader.CurrentDepth;
                while (reader.Read() && depth < reader.CurrentDepth)
                {
                }
            }
        }

        internal static string ReadAsString(ref Utf8JsonReader reader, byte[] propertyName)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new InvalidDataException($"Expected '{Encoding.UTF8.GetString(propertyName)}' to be of type {JsonTokenType.String}.");
            }

            return reader.GetString();
        }
    }
}