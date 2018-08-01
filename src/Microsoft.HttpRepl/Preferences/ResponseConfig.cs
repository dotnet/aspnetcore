// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public class ResponseConfig : RequestOrResponseConfig
    {
        public ResponseConfig(HttpState state)
            : base(state)
        {
        }

        public override AllowedColors BodyColor => State.GetColorPreference(WellKnownPreference.ResponseBodyColor, base.BodyColor);

        public override AllowedColors SchemeColor => State.GetColorPreference(WellKnownPreference.ResponseSchemeColor, base.SchemeColor);

        public override AllowedColors HeaderKeyColor => State.GetColorPreference(WellKnownPreference.ResponseHeaderKeyColor, base.HeaderKeyColor);

        public override AllowedColors HeaderSeparatorColor => State.GetColorPreference(WellKnownPreference.ResponseHeaderSeparatorColor, base.HeaderSeparatorColor);

        public override AllowedColors HeaderValueSeparatorColor => State.GetColorPreference(WellKnownPreference.ResponseHeaderValueSeparatorColor, base.HeaderValueSeparatorColor);

        public override AllowedColors HeaderValueColor => State.GetColorPreference(WellKnownPreference.ResponseHeaderValueColor, base.HeaderValueColor);

        public override AllowedColors HeaderColor => State.GetColorPreference(WellKnownPreference.ResponseHeaderColor, base.HeaderColor);

        public override AllowedColors GeneralColor => State.GetColorPreference(WellKnownPreference.ResponseColor, base.GeneralColor);

        public override AllowedColors ProtocolColor => State.GetColorPreference(WellKnownPreference.ResponseProtocolColor, base.ProtocolColor);

        public override AllowedColors ProtocolNameColor => State.GetColorPreference(WellKnownPreference.ResponseProtocolNameColor, base.ProtocolNameColor);

        public override AllowedColors ProtocolVersionColor => State.GetColorPreference(WellKnownPreference.ResponseProtocolVersionColor, base.ProtocolVersionColor);

        public override AllowedColors ProtocolSeparatorColor => State.GetColorPreference(WellKnownPreference.ResponseProtocolSeparatorColor, base.ProtocolSeparatorColor);

        public override AllowedColors StatusColor => State.GetColorPreference(WellKnownPreference.ResponseStatusColor, base.StatusColor);

        public override AllowedColors StatusCodeColor => State.GetColorPreference(WellKnownPreference.ResponseStatusCodeColor, base.StatusCodeColor);

        public override AllowedColors StatusReasonPhraseColor => State.GetColorPreference(WellKnownPreference.ResponseStatusReaseonPhraseColor, base.StatusReasonPhraseColor);
    }
}
