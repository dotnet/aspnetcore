// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationEvents method SigningIn.
    /// </summary>    
    public class CookieSigningInContext : BaseCookieContext
    {
        /// <summary>
        /// Creates a new instance of the context object.
        /// </summary>
        /// <param name="context">The HTTP request context</param>
        /// <param name="scheme">The scheme data</param>
        /// <param name="options">The handler options</param>
        /// <param name="principal">Initializes Principal property</param>
        /// <param name="properties">Initializes Extra property</param>
        /// <param name="cookieOptions">Initializes options for the authentication cookie.</param>
        public CookieSigningInContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CookieAuthenticationOptions options,
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            CookieOptions cookieOptions)
            : base(context, scheme, options, properties)
        {
            Principal = principal;
            CookieOptions = cookieOptions;
        }

        /// <summary>
        /// Contains the claims about to be converted into the outgoing cookie.
        /// May be replaced or altered during the SigningIn call.
        /// </summary>
        public ClaimsPrincipal Principal { get; set; }

        /// <summary>
        /// The options for creating the outgoing cookie.
        /// May be replace or altered during the SigningIn call.
        /// </summary>
        public CookieOptions CookieOptions { get; set; }
    }
}
