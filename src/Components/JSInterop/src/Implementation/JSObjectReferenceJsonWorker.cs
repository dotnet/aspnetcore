// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Implementation;

/// <summary>
/// Used by JsonConverters to read or write a <see cref="IJSObjectReference"/> instance.
/// <para>
/// This type is part of ASP.NET Core's internal infrastructure and is not recommended for use by external code.
/// </para>
/// </summary>
public static class JSObjectReferenceJsonWorker
{
    internal static readonly JsonEncodedText JSObjectIdKey = JsonEncodedText.Encode("__jsObjectId");

    /// <summary>
    /// Reads the id for a <see cref="JSObjectReference"/> instance.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/></param>
    /// <returns>The deserialized id for the <see cref="JSObjectReference"/>.</returns>
    public static long ReadJSObjectReferenceIdentifier(ref Utf8JsonReader reader)
    {
        long? id = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (id is null && reader.ValueTextEquals(JSObjectIdKey.EncodedUtf8Bytes))
                {
                    reader.Read();
                    id = reader.GetInt64();
                }
                else
                {
                    throw new JsonException($"Unexpected JSON property {reader.GetString()}.");
                }
            }
            else
            {
                throw new JsonException($"Unexpected JSON token {reader.TokenType}");
            }
        }

        if (!id.HasValue)
        {
            throw new JsonException($"Required property {JSObjectIdKey} not found.");
        }

        return id.Value;
    }

    /// <summary>
    /// Writes a <see cref="JSObjectReference"/> to the <paramref name="objectReference"/>.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/>.</param>
    /// <param name="objectReference">The <see cref="JSObjectReference"/> to write.</param>
    public static void WriteJSObjectReference(Utf8JsonWriter writer, JSObjectReference objectReference)
    {
        writer.WriteStartObject();
        writer.WriteNumber(JSObjectIdKey, objectReference.Id);
        writer.WriteEndObject();
    }
}
