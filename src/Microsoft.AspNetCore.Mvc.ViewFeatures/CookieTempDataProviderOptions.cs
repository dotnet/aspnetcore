// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for cookies set by <see cref="CookieTempDataProvider"/>
    /// </summary>
    public class CookieTempDataProviderOptions
    {
        private CookieBuilder _cookieBuilder = new CookieBuilder
        {
            Name = CookieTempDataProvider.CookieName,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,

            // Some browsers do not allow non-secure endpoints to set cookies with a 'secure' flag or overwrite cookies
            // whose 'secure' flag is set (http://httpwg.org/http-extensions/draft-ietf-httpbis-cookie-alone.html).
            // Since mixing secure and non-secure endpoints is a common scenario in applications, we are relaxing the
            // restriction on secure policy on some cookies by setting to 'None'. Cookies related to authentication or
            // authorization use a stronger policy than 'None'.
            SecurePolicy = CookieSecurePolicy.None,
        };

        /// <summary>
        /// <para>
        /// Determines the settings used to create the cookie in <see cref="CookieTempDataProvider"/>.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Strict"/>.
        /// <see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.SameAsRequest" />.
        /// <see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>
        /// </para>
        /// </summary>
        public CookieBuilder Cookie
        {
            get => _cookieBuilder;
            set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        #region Obsolete API
        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Path"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// The path set on the cookie. If set to <c>null</c>, the "path" attribute on the cookie is set to the current
        /// request's <see cref="HttpRequest.PathBase"/> value. If the value of <see cref="HttpRequest.PathBase"/> is
        /// <c>null</c> or empty, then the "path" attribute is set to the value of <see cref="CookieOptions.Path"/>.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Path) + ".")]
        public string Path { get => Cookie.Path; set => Cookie.Path = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Domain"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// The domain set on a cookie. Defaults to <c>null</c>.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Domain) + ".")]
        public string Domain { get => Cookie.Domain; set => Cookie.Domain = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Name"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// The name of the cookie which stores TempData. Defaults to <see cref="CookieTempDataProvider.CookieName"/>. 
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Name) + ".")]
        public string CookieName { get; set; } = CookieTempDataProvider.CookieName;
        #endregion
    }
}
