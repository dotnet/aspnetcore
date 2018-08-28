// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public static class WellKnownPreference
    {
        public static class Catalog
        {
            private static IReadOnlyList<string> _names;

            public static IReadOnlyList<string> Names
            {
                get
                {
                    if (_names != null)
                    {
                        return _names;
                    }

                    List<string> matchingProperties = new List<string>();

                    foreach (PropertyInfo property in typeof(WellKnownPreference).GetProperties(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (property.PropertyType == typeof(string) && property.GetMethod != null && property.GetValue(null) is string val)
                        {
                            matchingProperties.Add(val);
                        }
                    }

                    return _names = matchingProperties;
                }
            }
        }

        public static string JsonArrayBraceColor { get; } = "colors.json.arrayBrace";

        public static string JsonObjectBraceColor { get; } = "colors.json.objectBrace";

        public static string JsonNameColor { get; } = "colors.json.name";

        public static string JsonNameSeparatorColor { get; } = "colors.json.nameSeparator";

        public static string JsonIndentSize { get; } = "formatting.json.indentSize";

        public static string JsonCommaColor { get; } = "colors.json.comma";

        public static string JsonLiteralColor { get; } = "colors.json.literal";

        public static string JsonNullColor { get; } = "colors.json.null";

        public static string JsonBoolColor { get; } = "colors.json.bool";

        public static string JsonNumericColor { get; } = "colors.json.numeric";

        public static string JsonStringColor { get; } = "colors.json.string";

        public static string JsonColor { get; } = "colors.json";

        public static string JsonSyntaxColor { get; } = "colors.json.syntax";

        public static string JsonBraceColor { get; } = "colors.json.brace";

        public static string RequestColor { get; } = "colors.request";

        public static string RequestBodyColor { get; } = "colors.request.body";

        public static string RequestSchemeColor { get; } = "colors.request.scheme";

        public static string RequestHeaderKeyColor { get; } = "colors.request.header.key";

        public static string RequestHeaderSeparatorColor { get; } = "colors.request.header.separator";

        public static string RequestHeaderValueSeparatorColor { get; } = "colors.request.header.valueSeparator";

        public static string RequestHeaderValueColor { get; } = "colors.request.header.value";

        public static string RequestHeaderColor { get; } = "colors.request.header";

        public static string RequestProtocolColor { get; } = "colors.request.protocol";

        public static string RequestProtocolNameColor { get; } = "colors.request.protocol.name";

        public static string RequestProtocolSeparatorColor { get; } = "colors.request.protocol.separator";

        public static string RequestProtocolVersionColor { get; } = "colors.request.protocol.version";

        public static string RequestStatusColor { get; } = "colors.request.status";

        public static string RequestStatusCodeColor { get; } = "colors.request.status.code";

        public static string RequestStatusReaseonPhraseColor { get; } = "colors.request.status.reasonPhrase";

        public static string RequestMethodColor { get; } = "colors.request.method";

        public static string RequestAddressColor { get; } = "colors.request.address";


        public static string ResponseColor { get; } = "colors.response";

        public static string ResponseBodyColor { get; } = "colors.response.body";

        public static string ResponseSchemeColor { get; } = "colors.response.scheme";

        public static string ResponseHeaderKeyColor { get; } = "colors.response.header.key";

        public static string ResponseHeaderSeparatorColor { get; } = "colors.response.header.separator";

        public static string ResponseHeaderValueSeparatorColor { get; } = "colors.response.header.valueSeparator";

        public static string ResponseHeaderValueColor { get; } = "colors.response.header.value";

        public static string ResponseHeaderColor { get; } = "colors.response.header";

        public static string ResponseProtocolColor { get; } = "colors.response.protocol";

        public static string ResponseProtocolNameColor { get; } = "colors.response.protocol.name";

        public static string ResponseProtocolSeparatorColor { get; } = "colors.response.protocol.separator";

        public static string ResponseProtocolVersionColor { get; } = "colors.response.protocol.version";

        public static string ResponseStatusColor { get; } = "colors.response.status";

        public static string ResponseStatusCodeColor { get; } = "colors.response.status.code";

        public static string ResponseStatusReaseonPhraseColor { get; } = "colors.response.status.reasonPhrase";

        public static string RequestOrResponseColor { get; } = "colors.requestOrResponse";

        public static string ErrorColor { get; } = "colors.error";

        public static string WarningColor { get; } = "colors.warning";

        public static string BodyColor { get; } = "colors.body";

        public static string SchemeColor { get; } = "colors.scheme";

        public static string HeaderKeyColor { get; } = "colors.header.key";

        public static string HeaderSeparatorColor { get; } = "colors.header.separator";

        public static string HeaderValueSeparatorColor { get; } = "colors.header.valueSeparator";

        public static string HeaderValueColor { get; } = "colors.header.value";

        public static string HeaderColor { get; } = "colors.header";

        public static string ProtocolColor { get; } = "colors.protocol";

        public static string ProtocolNameColor { get; } = "colors.protocol.name";

        public static string ProtocolSeparatorColor { get; } = "colors.protocol.separator";

        public static string ProtocolVersionColor { get; } = "colors.protocol.version";

        public static string StatusColor { get; } = "colors.status";

        public static string StatusCodeColor { get; } = "colors.status.code";

        public static string StatusReaseonPhraseColor { get; } = "colors.status.reasonPhrase";


        public static string DefaultEditorCommand { get; } = "editor.command.default";

        public static string DefaultEditorArguments { get; } = "editor.command.default.arguments";

        public static string SwaggerRequeryBehavior { get; } = "swagger.requery";


        public static AllowedColors GetColorPreference(this HttpState programState, string preference, AllowedColors defaultvalue = AllowedColors.None)
        {
            if (!programState.Preferences.TryGetValue(preference, out string preferenceValueString) || !Enum.TryParse(preferenceValueString, true, out AllowedColors result))
            {
                result = defaultvalue;
            }

            return result;
        }

        public static int GetIntPreference(this HttpState programState, string preference, int defaultValue = 0)
        {
            if (!programState.Preferences.TryGetValue(preference, out string preferenceValueString) || !int.TryParse(preferenceValueString, out int result))
            {
                result = defaultValue;
            }

            return result;
        }

        public static string GetStringPreference(this HttpState programState, string preference, string defaultValue = null)
        {
            if (!programState.Preferences.TryGetValue(preference, out string result))
            {
                result = defaultValue;
            }

            return result;
        }
    }
}
