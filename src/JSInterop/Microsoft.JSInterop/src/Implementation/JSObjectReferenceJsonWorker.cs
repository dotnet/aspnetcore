// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.JSInterop.Implementation
{
    /// <summary>
    /// Used by JsonConverters to read or write a IJSObjectReference instance.
    /// <para>
    /// This type is part of ASP.NET Core's internal infrastructure and is not recommended for use by external code.
    /// </para>
    /// </summary>
    public static class JSObjectReferenceJsonWorker
    {
        private static readonly JsonEncodedText _idKey = JsonEncodedText.Encode("__jsObjectId");

        /// <summary>
        /// Reads the id for a <see cref="JSObjectReference"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/></param>
        /// <returns></returns>
        public static long ReadJSObjectReferenceIdentifier(ref Utf8JsonReader reader)
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

        /// <summary>
        /// Writes a <see cref="JSObjectReference"/> to the <paramref name="objectReference"/>.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/>.</param>
        /// <param name="objectReference">The <see cref="JSObjectReference"/> to write.</param>
        public static void WriteJSObjectReference(Utf8JsonWriter writer, JSObjectReference objectReference)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_idKey, objectReference.Id);
            writer.WriteEndObject();
        }
    }
}
