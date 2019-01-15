// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// An event context for RemoteSignOut.
    /// </summary>
    public class RemoteSignOutContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        /// <param name="message"></param>
        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, WsFederationMessage message)
            : base(context, scheme, options, new AuthenticationProperties())
            => ProtocolMessage = message;

        /// <summary>
        /// The signout message.
        /// </summary>
        public WsFederationMessage ProtocolMessage { get; set; }
    }
}