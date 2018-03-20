// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class JsonUtils
    {
        public static JObject GetObject(JToken token)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{token?.Type}'. Expected a JSON Object.");
            }

            return (JObject)token;
        }

        public static T GetOptionalProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None, T defaultValue = default)
        {
            var prop = json[property];

            if (prop == null)
            {
                return defaultValue;
            }

            return GetValue<T>(property, expectedType, prop);
        }

        public static T GetRequiredProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None)
        {
            var prop = json[property];

            if (prop == null)
            {
                throw new InvalidDataException($"Missing required property '{property}'.");
            }

            return GetValue<T>(property, expectedType, prop);
        }

        public static T GetValue<T>(string property, JTokenType expectedType, JToken prop)
        {
            if (expectedType != JTokenType.None && prop.Type != expectedType)
            {
                throw new InvalidDataException($"Expected '{property}' to be of type {expectedType}.");
            }
            return prop.Value<T>();
        }

        public static string GetTokenString(JsonToken tokenType)
        {
            switch (tokenType)
            {
                case JsonToken.None:
                    break;
                case JsonToken.StartObject:
                    return JTokenType.Object.ToString();
                case JsonToken.StartArray:
                    return JTokenType.Array.ToString();
                case JsonToken.PropertyName:
                    return JTokenType.Property.ToString();
                default:
                    break;
            }
            return tokenType.ToString();
        }

        public static int? ReadAsInt32(JsonTextReader reader, string propertyName)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.Integer)
            {
                throw new InvalidDataException($"Expected '{propertyName}' to be of type {JTokenType.Integer}.");
            }

            if (reader.Value == null)
            {
                return null;
            }

            return Convert.ToInt32(reader.Value);
        }

        public static string ReadAsString(JsonTextReader reader, string propertyName)
        {
            reader.Read();

            if (reader.TokenType != JsonToken.String)
            {
                throw new InvalidDataException($"Expected '{propertyName}' to be of type {JTokenType.String}.");
            }

            return reader.Value?.ToString();
        }

        public static bool CheckRead(JsonTextReader reader)
        {
            if (!reader.Read())
            {
                throw new JsonReaderException("Unexpected end when reading JSON");
            }

            return true;
        }
    }
}
