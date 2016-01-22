// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class AuthenticationValidatedContext : BaseOpenIdConnectContext
    {
        public AuthenticationValidatedContext(HttpContext context, OpenIdConnectOptions options, AuthenticationProperties properties)
            : base(context, options)
        {
            Properties = properties;
        }

        public AuthenticationProperties Properties { get; }

        public OpenIdConnectMessage TokenEndpointResponse { get; set; }
    }
}
