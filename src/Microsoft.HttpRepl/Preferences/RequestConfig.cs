// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public class RequestConfig : RequestOrResponseConfig
    {
        public RequestConfig(HttpState state)
            : base(state)
        {
        }

        public override AllowedColors BodyColor => State.GetColorPreference(WellKnownPreference.RequestBodyColor, base.BodyColor);

        public override AllowedColors SchemeColor => State.GetColorPreference(WellKnownPreference.RequestSchemeColor, base.SchemeColor);

        public override AllowedColors HeaderKeyColor => State.GetColorPreference(WellKnownPreference.RequestHeaderKeyColor, base.HeaderKeyColor);

        public override AllowedColors HeaderSeparatorColor => State.GetColorPreference(WellKnownPreference.RequestHeaderSeparatorColor, base.HeaderSeparatorColor);

        public override AllowedColors HeaderValueSeparatorColor => State.GetColorPreference(WellKnownPreference.RequestHeaderValueSeparatorColor, base.HeaderValueSeparatorColor);

        public override AllowedColors HeaderValueColor => State.GetColorPreference(WellKnownPreference.RequestHeaderValueColor, base.HeaderValueColor);

        public override AllowedColors HeaderColor => State.GetColorPreference(WellKnownPreference.RequestHeaderColor, base.HeaderColor);

        public override AllowedColors GeneralColor => State.GetColorPreference(WellKnownPreference.RequestColor, base.GeneralColor);

        public override AllowedColors ProtocolColor => State.GetColorPreference(WellKnownPreference.RequestProtocolColor, base.ProtocolColor);

        public override AllowedColors ProtocolNameColor => State.GetColorPreference(WellKnownPreference.RequestProtocolNameColor, base.ProtocolNameColor);

        public override AllowedColors ProtocolVersionColor => State.GetColorPreference(WellKnownPreference.RequestProtocolVersionColor, base.ProtocolVersionColor);

        public override AllowedColors ProtocolSeparatorColor => State.GetColorPreference(WellKnownPreference.RequestProtocolSeparatorColor, base.ProtocolSeparatorColor);

        public override AllowedColors StatusColor => State.GetColorPreference(WellKnownPreference.RequestStatusColor, base.StatusColor);

        public override AllowedColors StatusCodeColor => State.GetColorPreference(WellKnownPreference.RequestStatusCodeColor, base.StatusCodeColor);

        public override AllowedColors StatusReasonPhraseColor => State.GetColorPreference(WellKnownPreference.RequestStatusReaseonPhraseColor, base.StatusReasonPhraseColor);

        public AllowedColors MethodColor => State.GetColorPreference(WellKnownPreference.RequestMethodColor, GeneralColor);

        public AllowedColors AddressColor => State.GetColorPreference(WellKnownPreference.RequestAddressColor, GeneralColor);
    }
}
