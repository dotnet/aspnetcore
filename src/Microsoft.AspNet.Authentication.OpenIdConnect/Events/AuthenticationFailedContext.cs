// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    public class AuthenticationFailedContext : BaseControlContext<OpenIdConnectOptions>
    {
        public AuthenticationFailedContext(HttpContext context, OpenIdConnectOptions options)
            : base(context, options)
        {
        }

        public Exception Exception { get; set; }

        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}