// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.OpenIdConnectBearer
{
    public class SecurityTokenReceivedContext : BaseControlContext<OpenIdConnectBearerOptions>
    {
        public SecurityTokenReceivedContext(HttpContext context, OpenIdConnectBearerOptions options)
            : base(context, options)
        {
        }

        public string SecurityToken { get; set; }
    }
}
