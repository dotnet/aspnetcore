// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// An object representing a CSP violation report.
    /// </summary>
    public class CspReport
    {
        [JsonPropertyName("csp-report")]
        public Report ReportData { get; set; }
        public class Report
        {
            [JsonPropertyName("blocked-uri")]
            public string BlockedUri { get; set; }
            [JsonPropertyName("document-uri")]
            public string DocumentUri { get; set; }
            [JsonPropertyName("referrer")]
            public string Referrer { get; set; }
            [JsonPropertyName("violated-directive")]
            public string ViolatedDirective { get; set; }
            [JsonPropertyName("source-file")]
            public string SourceFile { get; set; }
            [JsonPropertyName("line-number")]
            [JsonConverter(typeof(NumberToStringConverter))]
            public string LineNumber { get; set; }

            // Old browsers don't set the next two fields (e.g. Firefox v25/v26)
            [JsonPropertyName("original-policy")]
            public string OriginalPolicy { get; set; }
            [JsonPropertyName("effective-directive")]
            public string EffectiveDirective { get; set; }

            // CSP3 only
            [JsonPropertyName("script-sample")]
            public string ScriptSample { get; set; }
            [JsonPropertyName("disposition")]
            public string Disposition { get; set; }
        }
    }

    class NumberToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32().ToString();
            }

            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
