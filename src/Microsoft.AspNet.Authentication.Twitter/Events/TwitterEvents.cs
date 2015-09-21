// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.Twitter
{
    /// <summary>
    /// Default <see cref="ITwitterEvents"/> implementation.
    /// </summary>
    public class TwitterEvents : ITwitterEvents
    {
        /// <summary>
        /// Gets or sets the function that is invoked when the Authenticated method is invoked.
        /// </summary>
        public Func<TwitterCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Gets or sets the function that is invoked when the ReturnEndpoint method is invoked.
        /// </summary>
        public Func<SigningInContext, Task> OnSigningIn { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Gets or sets the delegate that is invoked when the ApplyRedirect method is invoked.
        /// </summary>
        public Func<TwitterRedirectToAuthorizationEndpointContext, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.FromResult(0);
        };

        /// <summary>
        /// Invoked whenever Twitter successfully authenticates a user
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task CreatingTicket(TwitterCreatingTicketContext context) => OnCreatingTicket(context);

        /// <summary>
        /// Invoked prior to the <see cref="System.Security.Claims.ClaimsIdentity"/> being saved in a local cookie and the browser being redirected to the originally requested URL.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task SigningIn(SigningInContext context) => OnSigningIn(context);

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the Twitter middleware
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge </param>
        public virtual Task RedirectToAuthorizationEndpoint(TwitterRedirectToAuthorizationEndpointContext context) => OnRedirectToAuthorizationEndpoint(context);
    }
}
