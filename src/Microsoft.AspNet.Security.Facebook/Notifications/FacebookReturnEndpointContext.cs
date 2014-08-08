// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Facebook
{
    /// <summary>
    /// Provides context information to middleware providers.
    /// </summary>
    public class FacebookReturnEndpointContext : ReturnEndpointContext
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context">The http environment</param>
        /// <param name="ticket">The authentication ticket</param>
        public FacebookReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            : base(context, ticket)
        {
        }
    }
}
