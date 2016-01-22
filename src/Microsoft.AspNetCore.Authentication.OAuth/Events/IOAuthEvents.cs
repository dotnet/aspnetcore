// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Specifies callback methods which the <see cref="OAuthMiddleware"/> invokes to enable developer control over the authentication process.
    /// </summary>
    public interface IOAuthEvents : IRemoteAuthenticationEvents
    {
        /// <summary>
        /// Invoked after the provider successfully authenticates a user. This can be used to retrieve user information.
        /// This event may not be invoked by sub-classes of OAuthAuthenticationHandler if they override CreateTicketAsync.
        /// </summary>
        /// <param name="context">Contains information about the login session.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task CreatingTicket(OAuthCreatingTicketContext context);

        /// <summary>
        /// Called when a Challenge causes a redirect to the authorize endpoint.
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
        Task RedirectToAuthorizationEndpoint(OAuthRedirectToAuthorizationContext context);
    }
}
