// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// The context object used for <see cref="WsFederationEvents.MessageReceived"/>.
    /// </summary>
    public class MessageReceivedContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        /// <param name="properties"></param>
        public MessageReceivedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            WsFederationOptions options,
            AuthenticationProperties properties)
            : base(context, scheme, options, properties) { }

        /// <summary>
        /// The <see cref="WsFederationMessage"/> received on this request.
        /// </summary>
        public WsFederationMessage ProtocolMessage { get; set; }
    }
}