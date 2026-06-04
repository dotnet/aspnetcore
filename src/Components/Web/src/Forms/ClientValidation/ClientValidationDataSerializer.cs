// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

internal static class ClientValidationDataSerializer
{
    public static string Serialize(ClientValidationFormDescriptor form)
    {
        var buffer = new ArrayBufferWriter<byte>(initialCapacity: 256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields"u8);
            writer.WriteStartArray();
            foreach (var field in form.Fields)
            {
                WriteField(writer, field);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        // Utf8JsonWriter's default encoder escapes <, >, &, ' - safe for HTML text content.
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteField(Utf8JsonWriter writer, ClientValidationFieldDescriptor field)
    {
        writer.WriteStartObject();
        writer.WriteString("name"u8, field.Name);
        writer.WritePropertyName("rules"u8);
        writer.WriteStartArray();
        foreach (var rule in field.Rules)
        {
            WriteRule(writer, rule);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteRule(Utf8JsonWriter writer, ClientValidationRule rule)
    {
        writer.WriteStartObject();
        writer.WriteString("name"u8, rule.Name);
        writer.WriteString("message"u8, rule.ErrorMessage);
        if (rule.Parameters is { Count: > 0 } parameters)
        {
            writer.WritePropertyName("params"u8);
            writer.WriteStartObject();
            foreach (var kvp in parameters)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}
