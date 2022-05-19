// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.Razor.Serialization;

internal sealed class RazorDiagnosticJsonConverter : JsonConverter
{
    public static readonly RazorDiagnosticJsonConverter Instance = new RazorDiagnosticJsonConverter();
    private const string RazorDiagnosticMessageKey = "Message";

    public override bool CanConvert(Type objectType)
    {
        return typeof(RazorDiagnostic).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return null;
        }

        var diagnostic = JObject.Load(reader);
        var id = diagnostic[nameof(RazorDiagnostic.Id)].Value<string>();
        var severity = diagnostic[nameof(RazorDiagnostic.Severity)].Value<int>();
        var message = diagnostic[RazorDiagnosticMessageKey].Value<string>();

        var span = diagnostic[nameof(RazorDiagnostic.Span)].Value<JObject>();
        var filePath = span[nameof(SourceSpan.FilePath)].Value<string>();
        var absoluteIndex = span[nameof(SourceSpan.AbsoluteIndex)].Value<int>();
        var lineIndex = span[nameof(SourceSpan.LineIndex)].Value<int>();
        var characterIndex = span[nameof(SourceSpan.CharacterIndex)].Value<int>();
        var length = span[nameof(SourceSpan.Length)].Value<int>();

        var descriptor = new RazorDiagnosticDescriptor(id, () => message, (RazorDiagnosticSeverity)severity);
        var sourceSpan = new SourceSpan(filePath, absoluteIndex, lineIndex, characterIndex, length);

        return RazorDiagnostic.Create(descriptor, sourceSpan);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var diagnostic = (RazorDiagnostic)value;

        writer.WriteStartObject();
        WriteProperty(writer, nameof(RazorDiagnostic.Id), diagnostic.Id);
        WriteProperty(writer, nameof(RazorDiagnostic.Severity), (int)diagnostic.Severity);
        WriteProperty(writer, RazorDiagnosticMessageKey, diagnostic.GetMessage(CultureInfo.CurrentCulture));

        writer.WritePropertyName(nameof(RazorDiagnostic.Span));
        writer.WriteStartObject();
        WriteProperty(writer, nameof(SourceSpan.FilePath), diagnostic.Span.FilePath);
        WriteProperty(writer, nameof(SourceSpan.AbsoluteIndex), diagnostic.Span.AbsoluteIndex);
        WriteProperty(writer, nameof(SourceSpan.LineIndex), diagnostic.Span.LineIndex);
        WriteProperty(writer, nameof(SourceSpan.CharacterIndex), diagnostic.Span.CharacterIndex);
        WriteProperty(writer, nameof(SourceSpan.Length), diagnostic.Span.Length);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    private void WriteProperty<T>(JsonWriter writer, string key, T value)
    {
        writer.WritePropertyName(key);
        writer.WriteValue(value);
    }
}
