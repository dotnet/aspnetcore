// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;

namespace Microsoft.AspNet.Authentication.Cookies
{
    /// <summary>
    /// Context object passed to the ICookieAuthenticationProvider method ResponseSignOut    
    /// </summary>
    public class CookieResponseSignOutContext : BaseContext<CookieAuthenticationOptions>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="cookieOptions"></param>
        public CookieResponseSignOutContext(HttpContext context, CookieAuthenticationOptions options, CookieOptions cookieOptions)
            : base(context, options)
        {
            CookieOptions = cookieOptions;
        }

        /// <summary>
        /// The options for creating the outgoing cookie.
        /// May be replace or altered during the ResponseSignOut call.
        /// </summary>
        public CookieOptions CookieOptions
        {
            get;
            set;
        }
    }
}
