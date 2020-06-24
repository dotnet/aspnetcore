// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    internal sealed class ElementReferenceJsonConverter : JsonConverter<ElementReference>
    {
        private static readonly JsonEncodedText IdProperty = JsonEncodedText.Encode("__internalId");

        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Instantiates a new element reference json converter using the given <see cref="IJSRuntime"/>.
        /// </summary>
        /// <param name="jsRuntime">
        /// The <see cref="IJSRuntime"/> used for instantiating <see cref="ElementReference"/> instances.
        /// </param>
        public ElementReferenceJsonConverter(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override ElementReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string id = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(IdProperty.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetString();
                    }
                    else
                    {
                        throw new JsonException($"Unexpected JSON property '{reader.GetString()}'.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexpected JSON Token {reader.TokenType}.");
                }
            }

            if (id is null)
            {
                throw new JsonException("__internalId is required.");
            }

            return new ElementReference(id, _jsRuntime);
        }

        public override void Write(Utf8JsonWriter writer, ElementReference value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(IdProperty, value.Id);
            writer.WriteEndObject();
        }
    }
}
