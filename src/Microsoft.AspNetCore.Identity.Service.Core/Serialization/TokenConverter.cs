// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Identity.Service.Serialization
{
    internal abstract class TokenConverter<TToken> : JsonConverter
        where TToken : Token
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TToken).Equals(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (existingValue != null)
            {
                throw new InvalidOperationException("Can't populate an existing object.");
            }

            if (!typeof(TToken).Equals(objectType))
            {
                throw new InvalidOperationException($"{objectType.Name} can't be deserialized by this converter.");
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidOperationException("Expected an object");
            }
            var codeClaims = new List<Claim>();
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    throw new InvalidOperationException("Expected a property");
                }
                var propertyName = (string)reader.Value;

                if (!reader.Read())
                {
                    throw new InvalidOperationException("Expected the property content");
                }

                switch (reader.TokenType)
                {
                    case JsonToken.None:
                    case JsonToken.StartArray:
                        var value = reader.ReadAsString();
                        while (reader.TokenType != JsonToken.EndArray)
                        {
                            codeClaims.Add(new Claim(propertyName, value));
                            value = reader.ReadAsString();
                        }
                        break;
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.String:
                    case JsonToken.Boolean:
                    case JsonToken.Date:
                        codeClaims.Add(new Claim(propertyName, (string)reader.Value));
                        break;
                    case JsonToken.EndArray:
                        break;
                    case JsonToken.Null:
                    case JsonToken.Undefined:
                        break;
                    case JsonToken.PropertyName:
                    case JsonToken.Raw:
                    case JsonToken.Bytes:
                    case JsonToken.StartObject:
                    case JsonToken.StartConstructor:
                    case JsonToken.EndConstructor:
                    case JsonToken.EndObject:
                    case JsonToken.Comment:
                    default:
                        throw new InvalidOperationException("Invalid token type");
                }
            }
            if (reader.TokenType == JsonToken.EndObject)
            {
                return CreateToken(codeClaims);
            }
            else
            {
                throw new InvalidOperationException("Failed to read the object");
            }
        }

        public abstract TToken CreateToken(IEnumerable<Claim> claims);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var code = value as TToken;
            var objectType = code?.GetType();
            if (!typeof(TToken).Equals(objectType))
            {
                throw new InvalidOperationException($"{objectType.Name} can't be deserialized by this converter.");
            }

            var claimsArray = code
                .Where(c => c.Value != null)
                .OrderBy(c => c.Type, StringComparer.Ordinal).ToArray();

            writer.WriteStartObject();
            var i = 0;
            while (i < claimsArray.Length)
            {
                var j = i + 1;
                while (j < claimsArray.Length)
                {
                    if (!string.Equals(claimsArray[i].Type, claimsArray[j].Type, StringComparison.Ordinal))
                    {
                        break;
                    }
                    j++;
                }

                writer.WritePropertyName(claimsArray[i].Type);
                if (j - i == 1)
                {
                    serializer.Serialize(writer, claimsArray[i].Value);
                }
                else
                {
                    writer.WriteStartArray();
                    for (int k = i; k < j; k++)
                    {
                        serializer.Serialize(writer, claimsArray[k].Value);
                    }
                    writer.WriteEndArray();
                }

                i = j;
            }
            writer.WriteEndObject();
        }
    }
}
