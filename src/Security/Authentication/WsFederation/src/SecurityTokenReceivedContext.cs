// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class SecurityTokenReceivedContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        /// <summary>
        /// Creates a <see cref="SecurityTokenReceivedContext"/>
        /// </summary>
        public SecurityTokenReceivedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
        {
        }

        /// <summary>
        /// The <see cref="WsFederationMessage"/> received on this request.
        /// </summary>
        public WsFederationMessage ProtocolMessage { get; set; }
    }
}
