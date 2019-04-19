// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Options class provides information needed to control Bearer Authentication handler behavior
    /// </summary>
    public class NegotiateOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the challenge to put in the "WWW-Authenticate" header.
        /// </summary>
        public string Challenge { get; set; } = NegotiateDefaults.AuthenticationScheme;

        /// <summary>
        /// The object provided by the application to process events raised by the bearer authentication handler.
        /// The application may implement the interface fully, or it may create an instance of NegotiateEvents
        /// and assign delegates only to the events it wants to process.
        /// </summary>
        public new NegotiateEvents Events
        {
            get { return (NegotiateEvents)base.Events; }
            set { base.Events = value; }
        }

        internal INegotiateStateFactory StateFactory { get; set; } = new ReflectedNegotiateStateFactory();
    }
}
