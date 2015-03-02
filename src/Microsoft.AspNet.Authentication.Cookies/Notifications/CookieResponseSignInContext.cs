// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationProvider method ResponseSignIn.
    /// </summary>    
    public class CookieResponseSignInContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of the context object.
        /// </summary>
        /// <param name="context">The HTTP request context</param>
        /// <param name="options">The middleware options</param>
        /// <param name="authenticationScheme">Initializes AuthenticationScheme property</param>
        /// <param name="principal">Initializes Principal property</param>
        /// <param name="properties">Initializes Extra property</param>
        /// <param name="cookieOptions">Initializes options for the authentication cookie.</param>
        public CookieResponseSignInContext(
            HttpContext context,
            CookieAuthenticationOptions options,
            string authenticationScheme,
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            CookieOptions cookieOptions)
            : base(context, options)
        {
            AuthenticationScheme = authenticationScheme;
            Principal = principal;
            Properties = properties;
            CookieOptions = cookieOptions;
        }

        /// <summary>
        /// The name of the AuthenticationScheme creating a cookie
        /// </summary>
        public string AuthenticationScheme { get; private set; }

        /// <summary>
        /// Contains the claims about to be converted into the outgoing cookie.
        /// May be replaced or altered during the ResponseSignIn call.
        /// </summary>
        public ClaimsPrincipal Principal { get; set; }

        /// <summary>
        /// Contains the extra data about to be contained in the outgoing cookie.
        /// May be replaced or altered during the ResponseSignIn call.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <summary>
        /// The options for creating the outgoing cookie.
        /// May be replace or altered during the ResponseSignIn call.
        /// </summary>
        public CookieOptions CookieOptions { get; set; }
    }
}
