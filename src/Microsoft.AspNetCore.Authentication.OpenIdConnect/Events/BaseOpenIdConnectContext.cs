// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class BaseOpenIdConnectContext : BaseControlContext
    {
        public BaseOpenIdConnectContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options)
            : base(context)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        }

        public OpenIdConnectOptions Options { get; }

        public AuthenticationScheme Scheme { get; }

        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}