// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Default <see cref="IOAuthAuthenticationNotifications"/> implementation.
    /// </summary>
    public class OAuthAuthenticationNotifications : IOAuthAuthenticationNotifications
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthAuthenticationNotifications"/>
        /// </summary>
        public OAuthAuthenticationNotifications()
        {
            OnGetUserInformationAsync = OAuthAuthenticationDefaults.DefaultOnGetUserInformationAsync;
            OnReturnEndpoint = context => Task.FromResult(0);
            OnApplyRedirect = context => context.Response.Redirect(context.RedirectUri);
        }

        /// <summary>
        /// Gets or sets the function that is invoked when the Authenticated method is invoked.
        /// </summary>
        public Func<OAuthGetUserInformationContext, Task> OnGetUserInformationAsync { get; set; }

        /// <summary>
        /// Gets or sets the function that is invoked when the ReturnEndpoint method is invoked.
        /// </summary>
        public Func<OAuthReturnEndpointContext, Task> OnReturnEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the delegate that is invoked when the ApplyRedirect method is invoked.
        /// </summary>
        public Action<OAuthApplyRedirectContext> OnApplyRedirect { get; set; }

        /// <summary>
        /// Invoked after the provider successfully authenticates a user.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task GetUserInformationAsync(OAuthGetUserInformationContext context)
        {
            return OnGetUserInformationAsync(context);
        }

        /// <summary>
        /// Invoked prior to the <see cref="System.Security.Claims.ClaimsIdentity"/> being saved in a local cookie and the browser being redirected to the originally requested URL.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/></param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task ReturnEndpoint(OAuthReturnEndpointContext context)
        {
            return OnReturnEndpoint(context);
        }

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the OAuth middleware.
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
        public virtual void ApplyRedirect(OAuthApplyRedirectContext context)
        {
            OnApplyRedirect(context);
        }
    }
}
