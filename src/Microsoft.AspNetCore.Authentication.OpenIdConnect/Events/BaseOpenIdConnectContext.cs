// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class BaseOpenIdConnectContext : BaseControlContext
    {
        public BaseOpenIdConnectContext(HttpContext context, OpenIdConnectOptions options)
            : base(context)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options;
        }

        public OpenIdConnectOptions Options { get; }

        public OpenIdConnectMessage ProtocolMessage { get; set; }
    }
}