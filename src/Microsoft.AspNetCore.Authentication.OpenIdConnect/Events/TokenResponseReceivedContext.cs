// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// This Context can be used to be informed when an 'AuthorizationCode' is redeemed for tokens at the token endpoint.
    /// </summary>
    public class TokenResponseReceivedContext : BaseOpenIdConnectContext
    {
        /// <summary>
        /// Creates a <see cref="TokenResponseReceivedContext"/>
        /// </summary>
        public TokenResponseReceivedContext(HttpContext context, OpenIdConnectOptions options, AuthenticationProperties properties)
            : base(context, options)
        {
            Properties = properties;
        }

        public AuthenticationProperties Properties { get; }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/> that contains the tokens received after redeeming the code at the token endpoint.
        /// </summary>
        public OpenIdConnectMessage TokenEndpointResponse { get; set; }
    }
}
