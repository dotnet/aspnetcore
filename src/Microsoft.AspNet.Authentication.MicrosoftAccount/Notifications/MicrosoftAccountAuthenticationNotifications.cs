// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    /// <summary>
    /// Default <see cref="IMicrosoftAccountAuthenticationNotifications"/> implementation.
    /// </summary>
    public class MicrosoftAccountAuthenticationNotifications : OAuthAuthenticationNotifications, IMicrosoftAccountAuthenticationNotifications
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticationNotifications"/>
        /// </summary>
        public MicrosoftAccountAuthenticationNotifications()
        {
            OnAuthenticated = context => Task.FromResult(0);
        }

        /// <summary>
        /// Gets or sets the function that is invoked when the Authenticated method is invoked.
        /// </summary>
        public Func<MicrosoftAccountAuthenticatedContext, Task> OnAuthenticated { get; set; }

        /// <summary>
        /// Invoked whenever Microsoft succesfully authenticates a user
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        public virtual Task Authenticated(MicrosoftAccountAuthenticatedContext context)
        {
            return OnAuthenticated(context);
        }
    }
}
