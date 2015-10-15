// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Specifies callback methods which the <see cref="CookieAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
    /// </summary>
    public interface ICookieAuthenticationEvents
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
        Task SigningIn(CookieSigningInContext context);

        /// <summary>
        /// Called when an endpoint has provided sign in information after it is converted into a cookie.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        Task SignedIn(CookieSignedInContext context);

        /// <summary>
        /// Called when a SignOut causes a redirect in the cookie middleware.
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        Task RedirectToLogout(CookieRedirectContext context);

        /// <summary>
        /// Called when a SignIn causes a redirect in the cookie middleware.
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        Task RedirectToLogin(CookieRedirectContext context);

        /// <summary>
        /// Called when redirecting back to the return url in the cookie middleware.
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        Task RedirectToReturnUrl(CookieRedirectContext context);

        /// <summary>
        /// Called when an access denied causes a redirect in the cookie middleware.
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        Task RedirectToAccessDenied(CookieRedirectContext context);

        /// <summary>
        /// Called during the sign-out flow to augment the cookie cleanup process.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as information about the authentication cookie.</param>
        Task SigningOut(CookieSigningOutContext context);
    }
}
