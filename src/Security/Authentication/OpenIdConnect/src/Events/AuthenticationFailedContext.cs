// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// A conext for <see cref="OpenIdConnectEvents.AuthenticationFailed"/>.
    /// </summary>
    public class AuthenticationFailedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticationFailedContext"/>.
        /// </summary>
        /// <inheritdoc />
        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options)
            : base(context, scheme, options, new AuthenticationProperties())
        { }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        public OpenIdConnectMessage ProtocolMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception associated with the failure.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
