// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Used by JsonConverters to read or write a IJSObjectReference instance.
    /// </summary>
    internal static class JSObjectReferenceJsonWorker
    {
        private static readonly JsonEncodedText _idKey = JsonEncodedText.Encode("__jsObjectId");

        public static long ReadIdentifier(ref Utf8JsonReader reader)
        {
            long id = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (id == -1 && reader.ValueTextEquals(_idKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetInt64();
                    }
                    else
                    {
                        throw new JsonException($"Unexcepted JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexcepted JSON token {reader.TokenType}");
                }
            }

            if (id == -1)
            {
                throw new JsonException($"Required property {_idKey} not found.");
            }

            return id;
        }

        public static void Write(Utf8JsonWriter writer, long identifier)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_idKey, identifier);
            writer.WriteEndObject();
        }
    }
}
