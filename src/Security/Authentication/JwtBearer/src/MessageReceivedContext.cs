// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// A context for <see cref="JwtBearerEvents.OnMessageReceived"/>.
    /// </summary>
    public class MessageReceivedContext : ResultContext<JwtBearerOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MessageReceivedContext"/>.
        /// </summary>
        /// <inheritdoc />
        public MessageReceivedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            JwtBearerOptions options)
            : base(context, scheme, options) { }

        /// <summary>
        /// Bearer Token. This will give the application an opportunity to retrieve a token from an alternative location.
        /// </summary>
        public string? Token { get; set; }
    }
}
