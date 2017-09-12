// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// The context object used for <see cref="WsFederationEvents.SecurityTokenValidated"/>.
    /// </summary>
    public class SecurityTokenValidatedContext : RemoteAuthenticationContext<WsFederationOptions>
    {
        /// <summary>
        /// Creates a <see cref="SecurityTokenValidatedContext"/>
        /// </summary>
        public SecurityTokenValidatedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = principal;

        /// <summary>
        /// The <see cref="WsFederationMessage"/> received on this request.
        /// </summary>
        public WsFederationMessage ProtocolMessage { get; set; }

        /// <summary>
        /// The <see cref="SecurityToken"/> that was validated.
        /// </summary>
        public SecurityToken SecurityToken { get; set; }
    }
}