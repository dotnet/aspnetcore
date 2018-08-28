// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public class JsonConfig : IJsonConfig
    {
        private readonly HttpState _state;

        public int IndentSize => _state.GetIntPreference(WellKnownPreference.JsonIndentSize, 2);

        public AllowedColors DefaultColor => _state.GetColorPreference(WellKnownPreference.JsonColor);

        private AllowedColors DefaultBraceColor => _state.GetColorPreference(WellKnownPreference.JsonBraceColor, DefaultSyntaxColor);

        private AllowedColors DefaultSyntaxColor => _state.GetColorPreference(WellKnownPreference.JsonSyntaxColor, DefaultColor);

        private AllowedColors DefaultLiteralColor => _state.GetColorPreference(WellKnownPreference.JsonLiteralColor, DefaultColor);

        public AllowedColors ArrayBraceColor => _state.GetColorPreference(WellKnownPreference.JsonArrayBraceColor, DefaultBraceColor);

        public AllowedColors ObjectBraceColor => _state.GetColorPreference(WellKnownPreference.JsonObjectBraceColor, DefaultBraceColor);

        public AllowedColors CommaColor => _state.GetColorPreference(WellKnownPreference.JsonCommaColor, DefaultSyntaxColor);

        public AllowedColors NameColor => _state.GetColorPreference(WellKnownPreference.JsonNameColor, StringColor);

        public AllowedColors NameSeparatorColor => _state.GetColorPreference(WellKnownPreference.JsonNameSeparatorColor, DefaultSyntaxColor);

        public AllowedColors BoolColor => _state.GetColorPreference(WellKnownPreference.JsonBoolColor, DefaultLiteralColor);

        public AllowedColors NumericColor => _state.GetColorPreference(WellKnownPreference.JsonNumericColor, DefaultLiteralColor);

        public AllowedColors StringColor => _state.GetColorPreference(WellKnownPreference.JsonStringColor, DefaultLiteralColor);

        public AllowedColors NullColor => _state.GetColorPreference(WellKnownPreference.JsonNullColor, DefaultLiteralColor);

        public JsonConfig(HttpState state)
        {
            _state = state;
        }
    }
}
