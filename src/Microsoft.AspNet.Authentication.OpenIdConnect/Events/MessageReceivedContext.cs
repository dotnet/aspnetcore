// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    public class MessageReceivedContext : BaseControlContext<OpenIdConnectOptions>
    {
        public MessageReceivedContext(HttpContext context, OpenIdConnectOptions options)
            : base(context, options)
        {
        }

        public OpenIdConnectMessage ProtocolMessage { get; set; }

        /// <summary>
        /// Bearer Token. This will give application an opportunity to retrieve token from an alternation location.
        /// </summary>
        public string Token { get; set; }
    }
}