// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    public class RedirectFromIdentityProviderContext : BaseControlContext<OpenIdConnectAuthenticationOptions>
    {
        public RedirectFromIdentityProviderContext(HttpContext context, OpenIdConnectAuthenticationOptions options)
            : base(context, options)
        {
        }

        public string SignInScheme { get; set; }

        public bool IsRequestCompleted { get; set; }

        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}
