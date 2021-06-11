// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.JSInterop.Implementation
{
    /// <summary>
    /// Used by JsonConverters to read or write a <see cref="IJSObjectReference"/> or <see cref="IJSDataReference"/> instance.
    /// <para>
    /// This type is part of ASP.NET Core's internal infrastructure and is not recommended for use by external code.
    /// </para>
    /// </summary>
    public static class JSObjectReferenceJsonWorker
    {
        private static readonly JsonEncodedText _jsObjectIdKey = JsonEncodedText.Encode("__jsObjectId");
        private static readonly JsonEncodedText _jsDataReferenceLengthKey = JsonEncodedText.Encode("__jsDataReferenceLength");

        /// <summary>
        /// Reads the id for a <see cref="JSObjectReference"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/></param>
        /// <returns>The deserialized id and length for the <see cref="JSObjectReference"/>.</returns>
        public static DeserializedJSObjectReferenceValues ReadJSObjectReference(ref Utf8JsonReader reader)
        {
            long? id = null;
            long? length = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (id is null && reader.ValueTextEquals(_jsObjectIdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetInt64();
                    }
                    else if (length is null && reader.ValueTextEquals(_jsDataReferenceLengthKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        length = reader.GetInt64();
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

            if (!id.HasValue)
            {
                throw new JsonException($"Required property {_jsObjectIdKey} not found.");
            }

            return new DeserializedJSObjectReferenceValues() { Id = id.Value, Length = length };
        }

        /// <summary>
        /// Writes a <see cref="JSObjectReference"/> to the <paramref name="objectReference"/>.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/>.</param>
        /// <param name="objectReference">The <see cref="JSObjectReference"/> to write.</param>
        public static void WriteJSObjectReference(Utf8JsonWriter writer, JSObjectReference objectReference)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_jsObjectIdKey, objectReference.Id);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Contains the JSObjectReference.Id and Length that was deserialized
    /// </summary>
    public struct DeserializedJSObjectReferenceValues
    {
        /// <summary>
        /// Can be used for the <see cref="JSObjectReference.Id"/>
        /// </summary>
        public long Id;

        /// <summary>
        /// Can be used for the <see cref="JSDataReference.Length"/>
        /// </summary>
        public long? Length;
    }
}
