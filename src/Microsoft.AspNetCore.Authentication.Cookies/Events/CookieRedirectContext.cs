// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    /// <summary>
    /// Context passed when a Challenge, SignIn, or SignOut causes a redirect in the cookie middleware 
    /// </summary>
    public class CookieRedirectContext : BaseCookieContext
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context">The HTTP request context</param>
        /// <param name="options">The cookie middleware options</param>
        /// <param name="redirectUri">The initial redirect URI</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        public CookieRedirectContext(HttpContext context, CookieAuthenticationOptions options, string redirectUri, AuthenticationProperties properties)
            : base(context, options)
        {
            RedirectUri = redirectUri;
            Properties = properties;
        }

        /// <summary>
        /// Gets or Sets the URI used for the redirect operation.
        /// </summary>
        public string RedirectUri { get; set; }

        public AuthenticationProperties Properties { get; }
    }
}
