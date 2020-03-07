// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides programmatic configuration for the <see cref="CookiePolicyMiddleware"/>.
    /// </summary>
    public class CookiePolicyOptions
    {
        /// <summary>
        /// Affects the cookie's same site attribute.
        /// </summary>
        public SameSiteMode MinimumSameSitePolicy { get; set; } = SameSiteMode.Unspecified;

        /// <summary>
        /// Affects whether cookies must be HttpOnly.
        /// </summary>
        public HttpOnlyPolicy HttpOnly { get; set; } = HttpOnlyPolicy.None;

        /// <summary>
        /// Affects whether cookies must be Secure.
        /// </summary>
        public CookieSecurePolicy Secure { get; set; } = CookieSecurePolicy.None;

        public CookieBuilder ConsentCookie { get; set; } = new CookieBuilder()
        {
            Name = ".AspNet.Consent",
            Expiration = TimeSpan.FromDays(365),
            IsEssential = true,
        };

        /// <summary>
        /// Checks if consent policies should be evaluated on this request. The default is false.
        /// </summary>
        public Func<HttpContext, bool> CheckConsentNeeded { get; set; }

        /// <summary>
        /// Called when a cookie is appended.
        /// </summary>
        public Action<AppendCookieContext> OnAppendCookie { get; set; }

        /// <summary>
        /// Called when a cookie is deleted.
        /// </summary>
        public Action<DeleteCookieContext> OnDeleteCookie { get; set; }
    }
}
