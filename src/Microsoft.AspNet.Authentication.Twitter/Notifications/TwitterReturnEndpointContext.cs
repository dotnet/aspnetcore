// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.Twitter
{
    /// <summary>
    /// Provides context information to middleware providers.
    /// </summary>
    public class TwitterReturnEndpointContext : ReturnEndpointContext
    {
        /// <summary>
        /// Initializes a new <see cref="TwitterReturnEndpointContext"/>.
        /// </summary>
        /// <param name="context">HTTP environment</param>
        /// <param name="ticket">The authentication ticket</param>
        public TwitterReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            : base(context, ticket)
        {
        }
    }
}
