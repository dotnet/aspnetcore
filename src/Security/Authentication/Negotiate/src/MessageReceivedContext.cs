// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    public class MessageReceivedContext : ResultContext<NegotiateOptions>
    {
        public MessageReceivedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            NegotiateOptions options)
            : base(context, scheme, options) { }

        /// <summary>
        /// Bearer Token. This will give the application an opportunity to retrieve a token from an alternative location.
        /// </summary>
        public string Token { get; set; }
    }
}
