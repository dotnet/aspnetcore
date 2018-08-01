// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public abstract class RequestOrResponseConfig
    {
        protected HttpState State { get; }

        protected RequestOrResponseConfig(HttpState state)
        {
            State = state;
        }

        public virtual AllowedColors BodyColor => State.GetColorPreference(WellKnownPreference.BodyColor, GeneralColor);

        public virtual AllowedColors SchemeColor => State.GetColorPreference(WellKnownPreference.SchemeColor, GeneralColor);

        public virtual AllowedColors HeaderKeyColor => State.GetColorPreference(WellKnownPreference.HeaderKeyColor, HeaderColor);

        public virtual AllowedColors HeaderSeparatorColor => State.GetColorPreference(WellKnownPreference.HeaderSeparatorColor, HeaderColor);

        public virtual AllowedColors HeaderValueSeparatorColor => State.GetColorPreference(WellKnownPreference.HeaderValueSeparatorColor, HeaderSeparatorColor);

        public virtual AllowedColors HeaderValueColor => State.GetColorPreference(WellKnownPreference.HeaderValueColor, HeaderColor);

        public virtual AllowedColors HeaderColor => State.GetColorPreference(WellKnownPreference.HeaderColor, GeneralColor);

        public virtual AllowedColors GeneralColor => State.GetColorPreference(WellKnownPreference.RequestOrResponseColor);

        public virtual AllowedColors ProtocolColor => State.GetColorPreference(WellKnownPreference.ProtocolColor, GeneralColor);

        public virtual AllowedColors ProtocolNameColor => State.GetColorPreference(WellKnownPreference.ProtocolNameColor, ProtocolColor);

        public virtual AllowedColors ProtocolVersionColor => State.GetColorPreference(WellKnownPreference.ProtocolVersionColor, ProtocolColor);

        public virtual AllowedColors ProtocolSeparatorColor => State.GetColorPreference(WellKnownPreference.ProtocolSeparatorColor, ProtocolColor);

        public virtual AllowedColors StatusColor => State.GetColorPreference(WellKnownPreference.StatusColor, GeneralColor);

        public virtual AllowedColors StatusCodeColor => State.GetColorPreference(WellKnownPreference.StatusCodeColor, StatusColor);

        public virtual AllowedColors StatusReasonPhraseColor => State.GetColorPreference(WellKnownPreference.StatusReaseonPhraseColor, StatusColor);
    }
}
