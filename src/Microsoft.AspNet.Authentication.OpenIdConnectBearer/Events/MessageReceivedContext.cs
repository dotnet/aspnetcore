// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.OpenIdConnectBearer
{
    public class MessageReceivedContext : BaseControlContext<OpenIdConnectBearerOptions>
    {
        public MessageReceivedContext(HttpContext context, OpenIdConnectBearerOptions options)
            : base(context, options)
        {
        }

        /// <summary>
        /// Bearer Token. This will give application an opportunity to retrieve token from an alternation location.
        /// </summary>
        public string Token { get; set; }
    }
}