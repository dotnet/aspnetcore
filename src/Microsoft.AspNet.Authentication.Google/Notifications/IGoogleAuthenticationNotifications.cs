// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.Google
{
    /// <summary>
    /// Specifies callback methods which the <see cref="GoogleAuthenticationMiddleware" /> invokes to enable developer control over the authentication process.
    /// </summary>
    public interface IGoogleAuthenticationNotifications : IOAuthAuthenticationNotifications
    {
        /// <summary>
        /// Invoked whenever Google succesfully authenticates a user.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task Authenticated(GoogleAuthenticatedContext context);
    }
}
