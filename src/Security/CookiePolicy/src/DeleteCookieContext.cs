// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.CookiePolicy
{
    /// <summary>
    /// Context for <see cref="CookiePolicyOptions.OnDeleteCookie"/> that allows changes to the cookie prior to being deleted.
    /// </summary>
    public class DeleteCookieContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeleteCookieContext"/>.
        /// </summary>
        /// <param name="context">The request <see cref="HttpContext"/>.</param>
        /// <param name="options">The <see cref="Http.CookieOptions"/> passed to the cookie policy.</param>
        /// <param name="name">The cookie name to be deleted.</param>
        public DeleteCookieContext(HttpContext context, CookieOptions options, string name)
        {
            Context = context;
            CookieOptions = options;
            CookieName = name;
        }

        /// <summary>
        /// Gets the <see cref="HttpContext"/>.
        /// </summary>
        public HttpContext Context { get; }

        /// <summary>
        /// Gets the <see cref="Http.CookieOptions"/>.
        /// </summary>
        public CookieOptions CookieOptions { get; }

        /// <summary>
        /// Gets or sets the cookie name.
        /// </summary>
        public string CookieName { get; set; }

        /// <summary>
        /// Gets a value that determines if cookie consent is required before setting this cookie.
        /// </summary>
        public bool IsConsentNeeded { get; internal set; }

        /// <summary>
        /// Gets a value that determines if cookie consent was provided.
        /// </summary>
        public bool HasConsent { get; internal set; }

        /// <summary>
        /// Gets or sets a value that determines if the cookie can be deleted. If set to <see langword="false" />,
        /// cookie deletion is suppressed.
        /// </summary>
        public bool IssueCookie { get; set; }
    }
}
