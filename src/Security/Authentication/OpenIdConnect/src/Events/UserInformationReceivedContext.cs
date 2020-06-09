// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class UserInformationReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        public UserInformationReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties)
            : base(context, scheme, options, properties)
            => Principal = principal;

        public OpenIdConnectMessage ProtocolMessage { get; set; }

        public JsonDocument User { get; set; }
    }
}
