// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.MicrosoftAccount
{
    /// <summary>
    /// Default <see cref="IMicrosoftAccountAuthenticationNotifications"/> implementation.
    /// </summary>
    public class MicrosoftAccountAuthenticationNotifications : IMicrosoftAccountAuthenticationNotifications
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticationNotifications"/>
        /// </summary>
        public MicrosoftAccountAuthenticationNotifications()
        {
            OnAuthenticated = context => Task.FromResult(0);
            OnReturnEndpoint = context => Task.FromResult(0);
            OnApplyRedirect = context => context.Response.Redirect(context.RedirectUri);
        }

        /// <summary>
        /// Gets or sets the function that is invoked when the Authenticated method is invoked.
        /// </summary>
        public Func<MicrosoftAccountAuthenticatedContext, Task> OnAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the function that is invoked when the ReturnEndpoint method is invoked.
        /// </summary>
        public Func<MicrosoftAccountReturnEndpointContext, Task> OnReturnEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the delegate that is invoked when the ApplyRedirect method is invoked.
        /// </summary>
        public Action<MicrosoftAccountApplyRedirectContext> OnApplyRedirect { get; set; }

        /// <summary>
        /// Invoked whenever Microsoft succesfully authenticates a user
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task Authenticated(MicrosoftAccountAuthenticatedContext context)
        {
            return OnAuthenticated(context);
        }

        /// <summary>
        /// Invoked prior to the <see cref="System.Security.Claims.ClaimsIdentity"/> being saved in a local cookie and the browser being redirected to the originally requested URL.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/></param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task ReturnEndpoint(MicrosoftAccountReturnEndpointContext context)
        {
            return OnReturnEndpoint(context);
        }

        /// <summary>
        /// Called when a Challenge causes a redirect to authorize endpoint in the Microsoft account middleware.
        /// </summary>
        /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge.</param>
        public virtual void ApplyRedirect(MicrosoftAccountApplyRedirectContext context)
        {
            OnApplyRedirect(context);
        }
    }
}
