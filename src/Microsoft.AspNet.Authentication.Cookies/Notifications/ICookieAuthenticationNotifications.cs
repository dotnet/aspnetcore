// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Specifies callback methods which the <see cref="CookieAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
    /// </summary>
    public interface ICookieAuthenticationNotifications
    {
        /// <summary>
        /// Called each time a request principal has been validated by the middleware. By implementing this method the
        /// application may alter or reject the principal which has arrived with the request.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task ValidatePrincipal(CookieValidatePrincipalContext context);

        /// <summary>
        /// Called when an endpoint has provided sign in information before it is converted into a cookie. By
        /// implementing this method the claims and extra information that go into the ticket may be altered.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        void ResponseSignIn(CookieResponseSignInContext context);

        /// <summary>
        /// Called when an endpoint has provided sign in information after it is converted into a cookie.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        void ResponseSignedIn(CookieResponseSignedInContext context);

        /// <summary>
        /// Called when a Challenge, SignIn, or SignOut causes a redirect in the cookie middleware
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        void ApplyRedirect(CookieApplyRedirectContext context);

        /// <summary>
        /// Called during the sign-out flow to augment the cookie cleanup process.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as information about the authentication cookie.</param>
        void ResponseSignOut(CookieResponseSignOutContext context);

        /// <summary>
        /// Called when an exception occurs during request or response processing.
        /// </summary>
        /// <param name="context">Contains information about the exception that occurred</param>
        void Exception(CookieExceptionContext context);
    }
}
