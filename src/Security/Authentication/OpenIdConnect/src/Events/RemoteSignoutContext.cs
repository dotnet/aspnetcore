// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class RemoteSignOutContext : RemoteAuthenticationContext<OpenIdConnectOptions>
    {
        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage message)
            : base(context, scheme, options, new AuthenticationProperties())
            => ProtocolMessage = message;

        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}