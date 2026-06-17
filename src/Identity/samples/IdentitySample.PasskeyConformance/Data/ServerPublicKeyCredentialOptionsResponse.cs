// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IdentitySample.PasskeyConformance.Data;

[JsonConverter(typeof(JsonConverter))]
internal sealed class ServerPublicKeyCredentialOptionsResponse(string optionsJson) : OkResponse()
{
    public string OptionsJson { get; } = optionsJson;

    public sealed class JsonConverter : JsonConverter<ServerPublicKeyCredentialOptionsResponse>
    {
        public override ServerPublicKeyCredentialOptionsResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, ServerPublicKeyCredentialOptionsResponse value, JsonSerializerOptions options)
        {
            var optionsObject = JsonNode.Parse(value.OptionsJson)?.AsObject()
                ?? throw new JsonException("Could not parse the creation options JSON.");

            writer.WriteStartObject();
            writer.WriteString("status", value.Status);
            writer.WriteString("errorMessage", value.ErrorMessage);
            foreach (var (propertyName, propertyValue) in optionsObject)
            {
                writer.WritePropertyName(propertyName);
                if (propertyValue is not null)
                {
                    propertyValue.WriteTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
            writer.WriteEndObject();
        }
    }
}
