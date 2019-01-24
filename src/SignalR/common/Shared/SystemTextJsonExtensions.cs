// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Internal
{
    internal static class SystemTextJsonExtensions
    {
        public static bool CheckRead(this ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidDataException("Unexpected end when reading JSON.");
            }

            return true;
        }

        public static void EnsureObjectStart(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{reader.GetTokenString()}'. Expected a JSON Object.");
            }
        }

        public static string GetTokenString(this ref Utf8JsonReader reader)
        {
            return GetTokenString(reader.TokenType);
        }

        public static string GetTokenString(JsonTokenType tokenType)
        {
            switch (tokenType)
            {
                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    return "Object";
                case JsonTokenType.StartArray:
                    return "Array";
                case JsonTokenType.PropertyName:
                    return "Property";
                default:
                    break;
            }
            return tokenType.ToString();
        }

        public static void EnsureArrayStart(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{reader.GetTokenString()}'. Expected a JSON Array.");
            }
        }

        // Remove after https://github.com/dotnet/corefx/issues/33295 is done
        public static void Skip(this ref Utf8JsonReader reader)
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

        public static string ReadAsString(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Read();

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new InvalidDataException($"Expected '{propertyName}' to be of type {JsonTokenType.String}.");
            }

            return reader.GetString();
        }

        public static int? ReadAsInt32(this ref Utf8JsonReader reader, string propertyName)
        {
            reader.Read();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new InvalidDataException($"Expected '{propertyName}' to be of type {JsonTokenType.Number}.");
            }

            return reader.GetInt32();
        }

        public static object ReadAsType(this ref Utf8JsonReader reader, Type type)
        {
            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return reader.GetDateTime();
            }
            else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                return reader.GetDateTimeOffset();
            }
            else
            {
                reader.Read();

                var bytes = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                return JsonSerializer.Parse(bytes, type);
            }
        }
    }
}
