// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.OpenIdConnectBearer
{
    public class AuthenticationFailedContext : BaseControlContext<OpenIdConnectBearerOptions>
    {
        public AuthenticationFailedContext(HttpContext context, OpenIdConnectBearerOptions options)
            : base(context, options)
        {
        }

        public Exception Exception { get; set; }
    }
}