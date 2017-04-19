// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class MessageReceivedContext : BaseOpenIdConnectContext
    {
        public MessageReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// Bearer Token. This will give application an opportunity to retrieve token from an alternation location.
        /// </summary>
        public string Token { get; set; }

        public AuthenticationProperties Properties { get; set; }
    }
}