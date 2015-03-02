// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.Google
{
    /// <summary>
    /// The default <see cref="IGoogleAuthenticationNotifications"/> implementation.
    /// </summary>
    public class GoogleAuthenticationNotifications : OAuthAuthenticationNotifications, IGoogleAuthenticationNotifications
    {
        /// <summary>
        /// Initializes a new <see cref="GoogleAuthenticationNotifications"/>.
        /// </summary>
        public GoogleAuthenticationNotifications()
        {
            OnAuthenticated = context => Task.FromResult<object>(null);
        }

        /// <summary>
        /// Gets or sets the function that is invoked when the Authenticated method is invoked.
        /// </summary>
        public Func<GoogleAuthenticatedContext, Task> OnAuthenticated { get; set; }

        /// <summary>
        /// Invoked whenever Google succesfully authenticates a user.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task Authenticated(GoogleAuthenticatedContext context)
        {
            return OnAuthenticated(context);
        }
    }
}
