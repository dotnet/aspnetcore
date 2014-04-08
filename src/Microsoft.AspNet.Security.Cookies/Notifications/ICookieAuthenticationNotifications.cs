// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Cookies
{
    /// <summary>
    /// Specifies callback methods which the <see cref="CookieAuthenticationMiddleware"></see> invokes to enable developer control over the authentication process. />
    /// </summary>
    public interface ICookieAuthenticationNotifications
    {
        /// <summary>
        /// Called each time a request identity has been validated by the middleware. By implementing this method the
        /// application may alter or reject the identity which has arrived with the request.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
        Task ValidateIdentity(CookieValidateIdentityContext context);

        /// <summary>
        /// Called when an endpoint has provided sign in information before it is converted into a cookie. By
        /// implementing this method the claims and extra information that go into the ticket may be altered.
        /// </summary>
        /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
        void ResponseSignIn(CookieResponseSignInContext context);

        /// <summary>
        /// Called when a Challenge, SignIn, or SignOut causes a redirect in the cookie middleware
        /// </summary>
        /// <param name="context">Contains information about the event</param>
        void ApplyRedirect(CookieApplyRedirectContext context);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">Contains information about the login session as well as information about the authentication cookie.</param>
        void ResponseSignOut(CookieResponseSignOutContext context);
    }
}
