// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// A context for <see cref="OpenIdConnectEvents.UserInformationReceived(UserInformationReceivedContext)"/>.
    /// </summary>
    public class UserInformationReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UserInformationReceivedContext"/>.
        /// </summary>
        /// <inheritdoc />
        public UserInformationReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = principal;

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        public OpenIdConnectMessage ProtocolMessage { get; set; }

        /// <summary>
        /// Gets or sets the user information payload.
        /// </summary>
        public JsonDocument User { get; set; }
    }
}
