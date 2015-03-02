// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Base class used for certain event contexts
    /// </summary>
    public abstract class BaseValidatingTicketContext<TOptions> : BaseValidatingContext<TOptions>
    {
        /// <summary>
        /// Initializes base class used for certain event contexts
        /// </summary>
        protected BaseValidatingTicketContext(
            HttpContext context,
            TOptions options,
            AuthenticationTicket ticket)
            : base(context, options)
        {
            Ticket = ticket;
        }

        /// <summary>
        /// Contains the identity and properties for the application to authenticate. If the Validated method
        /// is invoked with an AuthenticationTicket or ClaimsIdentity argument, that new value is assigned to 
        /// this property in addition to changing IsValidated to true.
        /// </summary>
        public AuthenticationTicket Ticket { get; private set; }

        /// <summary>
        /// Replaces the ticket information on this context and marks it as as validated by the application. 
        /// IsValidated becomes true and HasError becomes false as a result of calling.
        /// </summary>
        /// <param name="ticket">Assigned to the Ticket property</param>
        /// <returns>True if the validation has taken effect.</returns>
        public bool Validated(AuthenticationTicket ticket)
        {
            Ticket = ticket;
            return Validated();
        }

        /// <summary>
        /// Alters the ticket information on this context and marks it as as validated by the application. 
        /// IsValidated becomes true and HasError becomes false as a result of calling.
        /// </summary>
        /// <param name="identity">Assigned to the Ticket.Identity property</param>
        /// <returns>True if the validation has taken effect.</returns>
        public bool Validated(ClaimsPrincipal principal)
        {
            AuthenticationProperties properties = Ticket != null ? Ticket.Properties : new AuthenticationProperties();
            // TODO: Ticket can be null, need to revisit
            return Validated(new AuthenticationTicket(principal, properties, Ticket.AuthenticationScheme));
        }
    }
}
