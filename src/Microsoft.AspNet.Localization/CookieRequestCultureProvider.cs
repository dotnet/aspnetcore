// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Determines the culture information for a request via the value of a cookie.
    /// </summary>
    public class CookieRequestCultureProvider : RequestCultureProvider
    {
        private static readonly char[] _cookieSeparator = new[] { '|' };
        private static readonly string _culturePrefix = "c=";
        private static readonly string _uiCulturePrefix = "uic=";
        
        /// <summary>
        /// Represent the default cookie name used to track the user's preferred culture information, which is "ASPNET_CULTURE".
        /// </summary>
        public static readonly string DefaultCookieName = "ASPNET_CULTURE";

        /// <summary>
        /// The name of the cookie that contains the user's preferred culture information.
        /// Defaults to <see cref="DefaultCookieName"/>.
        /// </summary>
        public string CookieName { get; set; } = DefaultCookieName;

        /// <inheritdoc />
        public override Task<RequestCulture> DetermineRequestCulture([NotNull] HttpContext httpContext)
        {
            var cookie = httpContext.Request.Cookies[CookieName];

            if (cookie == null)
            {
                return Task.FromResult((RequestCulture)null);
            }

            var requestCulture = ParseCookieValue(cookie);

            requestCulture = ValidateRequestCulture(requestCulture);

            return Task.FromResult(requestCulture);
        }

        /// <summary>
        /// Creates a string representation of a <see cref="RequestCulture"/> for placement in a cookie.
        /// </summary>
        /// <param name="requestCulture">The <see cref="RequestCulture"/>.</param>
        /// <returns>The cookie value.</returns>
        public static string MakeCookieValue([NotNull] RequestCulture requestCulture)
        {
            var seperator = _cookieSeparator[0].ToString();

            return string.Join(seperator,
                $"{_culturePrefix}{requestCulture.Culture.Name}",
                $"{_uiCulturePrefix}{requestCulture.UICulture.Name}");
        }

        /// <summary>
        /// Parses a <see cref="RequestCulture"/> from the specified cookie value.
        /// Returns <c>null</c> if parsing fails.
        /// </summary>
        /// <param name="value">The cookie value to parse.</param>
        /// <returns>The <see cref="RequestCulture"/> or <c>null</c> if parsing fails.</returns>
        public static RequestCulture ParseCookieValue([NotNull] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var parts = value.Split(_cookieSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return null;
            }

            var potentialCultureName = parts[0];
            var potentialUICultureName = parts[1];

            if (!potentialCultureName.StartsWith(_culturePrefix) || !potentialUICultureName.StartsWith(_uiCulturePrefix))
            {
                return null;
            }

            var cultureName = potentialCultureName.Substring(_culturePrefix.Length);
            var uiCultureName = potentialUICultureName.Substring(_uiCulturePrefix.Length);

            var culture = CultureInfoCache.GetCultureInfo(cultureName);
            var uiCulture = CultureInfoCache.GetCultureInfo(uiCultureName);

            if (culture == null || uiCulture == null)
            {
                return null;
            }

            return new RequestCulture(culture, uiCulture);
        }
    }
}
