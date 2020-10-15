// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// A context for <see cref="OpenIdConnectEvents.RemoteSignOut(RemoteSignOutContext)"/> event.
    /// </summary>
    public class RemoteSignOutContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RemoteSignOutContext"/>.
        /// </summary>
        /// <inheritdoc />
        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage message)
            : base(context, scheme, options, new AuthenticationProperties())
            => ProtocolMessage = message;

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}
