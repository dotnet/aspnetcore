// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationEvents method SigningOut    
    /// </summary>
    public class CookieSigningOutContext : BaseCookieContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cookieOptions"></param>
        public CookieSigningOutContext(
            HttpContext context, 
            CookieAuthenticationOptions options, 
            AuthenticationProperties properties, 
            CookieOptions cookieOptions)
            : base(context, options)
        {
            CookieOptions = cookieOptions;
            Properties = properties;
        }

        /// <summary>
        /// The options for creating the outgoing cookie.
        /// May be replace or altered during the SigningOut call.
        /// </summary>
        public CookieOptions CookieOptions { get; set; }

        public AuthenticationProperties Properties { get; set; }
    }
}
