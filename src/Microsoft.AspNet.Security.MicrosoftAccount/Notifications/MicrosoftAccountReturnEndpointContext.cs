// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Notifications;

namespace Microsoft.AspNet.Security.MicrosoftAccount
{
    /// <summary>
    /// Provides context information to middleware providers.
    /// </summary>
    public class MicrosoftAccountReturnEndpointContext : ReturnEndpointContext
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountReturnEndpointContext"/>.
        /// </summary>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="ticket">The authentication ticket.</param>
        public MicrosoftAccountReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            : base(context, ticket)
        {
        }
    }
}
