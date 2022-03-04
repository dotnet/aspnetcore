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

            // Check the comment on CookieBuilder below for more details
            SameSite = SameSiteMode.Lax,

            // This cookie has been marked as non-essential because a user could use the SessionStateTempDataProvider,
            // which is more common in production scenarios. Check the comment on CookieBuilder below
            // for more information.
            IsEssential = false,

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
        /// <see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Lax"/>. Setting this to
        /// <see cref="SameSiteMode.Strict"/> may cause browsers to not send back the cookie to the server in an
        /// OAuth login flow.
        /// <see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.SameAsRequest" />.
        /// <see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.
        /// <see cref="CookieBuilder.IsEssential"/> defaults to <c>false</c>, This property is only considered when a
        /// user opts into the CookiePolicyMiddleware. If you are using this middleware and want to use
        /// <see cref="CookieTempDataProvider"/>, then either set this property to <c>true</c> or
        /// request user consent for non-essential cookies.
        /// </para>
        /// </summary>
        public CookieBuilder Cookie
        {
            get => _cookieBuilder;
            set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
