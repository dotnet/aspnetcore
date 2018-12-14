// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class TokenValidatedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Creates a <see cref="TokenValidatedContext"/>
        /// </summary>
        public TokenValidatedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = principal;

        public OpenIdConnectMessage ProtocolMessage { get; set; }

        public JwtSecurityToken SecurityToken { get; set; }

        public OpenIdConnectMessage TokenEndpointResponse { get; set; }

        public string Nonce { get; set; }
    }
}