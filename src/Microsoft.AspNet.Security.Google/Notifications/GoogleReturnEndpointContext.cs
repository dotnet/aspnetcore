// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.Google
{
    /// <summary>
    /// Provides context information to middleware notifications.
    /// </summary>
    public class GoogleReturnEndpointContext : ReturnEndpointContext
    {
        /// <summary>
        /// Initialize a <see cref="GoogleReturnEndpointContext"/>
        /// </summary>
        /// <param name="context">HTTP environment</param>
        /// <param name="ticket">The authentication ticket</param>
        public GoogleReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            : base(context, ticket)
        {
        }
    }
}
