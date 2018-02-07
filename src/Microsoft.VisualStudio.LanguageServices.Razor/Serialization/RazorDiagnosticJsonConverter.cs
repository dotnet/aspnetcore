// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    internal class RazorDiagnosticJsonConverter : JsonConverter
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
            var span = diagnostic[nameof(RazorDiagnostic.Span)].Value<JObject>();
            var absoluteIndex = span[nameof(SourceSpan.AbsoluteIndex)].Value<int>();
            var lineIndex = span[nameof(SourceSpan.LineIndex)].Value<int>();
            var characterIndex = span[nameof(SourceSpan.CharacterIndex)].Value<int>();
            var length = span[nameof(SourceSpan.Length)].Value<int>();
            var filePath = span[nameof(SourceSpan.FilePath)].Value<string>();
            var message = diagnostic[RazorDiagnosticMessageKey].Value<string>();
            var id = diagnostic[nameof(RazorDiagnostic.Id)].Value<string>();
            var severity = diagnostic[nameof(RazorDiagnostic.Severity)].Value<int>();

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
            WriteProperty(writer, RazorDiagnosticMessageKey, diagnostic.GetMessage());

            writer.WritePropertyName(nameof(RazorDiagnostic.Span));
            serializer.Serialize(writer, diagnostic.Span);

            writer.WriteEndObject();
        }

        private void WriteProperty<T>(JsonWriter writer, string key, T value)
        {
            writer.WritePropertyName(key);
            writer.WriteValue(value);
        }
    }
}