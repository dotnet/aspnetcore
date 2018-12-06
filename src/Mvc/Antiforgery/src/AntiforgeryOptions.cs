// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery
{
    /// <summary>
    /// Provides programmatic configuration for the antiforgery token system.
    /// </summary>
    public class AntiforgeryOptions
    {
        private const string AntiforgeryTokenFieldName = "__RequestVerificationToken";
        private const string AntiforgeryTokenHeaderName = "RequestVerificationToken";

        private string _formFieldName = AntiforgeryTokenFieldName;

        private CookieBuilder _cookieBuilder = new CookieBuilder
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,

            // Check the comment on CookieBuilder for more details
            IsEssential = true,

            // Some browsers do not allow non-secure endpoints to set cookies with a 'secure' flag or overwrite cookies
            // whose 'secure' flag is set (http://httpwg.org/http-extensions/draft-ietf-httpbis-cookie-alone.html).
            // Since mixing secure and non-secure endpoints is a common scenario in applications, we are relaxing the
            // restriction on secure policy on some cookies by setting to 'None'. Cookies related to authentication or
            // authorization use a stronger policy than 'None'.
            SecurePolicy = CookieSecurePolicy.None,
        };

        /// <summary>
        /// The default cookie prefix, which is ".AspNetCore.Antiforgery.".
        /// </summary>
        public static readonly string DefaultCookiePrefix = ".AspNetCore.Antiforgery.";

        /// <summary>
        /// Determines the settings used to create the antiforgery cookies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If an explicit <see cref="CookieBuilder.Name"/> is not provided, the system will automatically generate a
        /// unique name that begins with <see cref="DefaultCookiePrefix"/>.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Strict"/>.
        /// <see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.
        /// <see cref="CookieBuilder.IsEssential"/> defaults to <c>true</c>. The cookie used by the antiforgery system
        /// is part of a security system that is necessary when using cookie-based authentication. It should be
        /// considered required for the application to function.
        /// <see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.None"/>.
        /// </para>
        /// </remarks>
        public CookieBuilder Cookie
        {
            get => _cookieBuilder;
            set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Specifies the name of the antiforgery token field that is used by the antiforgery system.
        /// </summary>
        public string FormFieldName
        {
            get => _formFieldName;
            set => _formFieldName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Specifies the name of the header value that is used by the antiforgery system. If <c>null</c> then
        /// antiforgery validation will only consider form data.
        /// </summary>
        public string HeaderName { get; set; } = AntiforgeryTokenHeaderName;

        /// <summary>
        /// Specifies whether to suppress the generation of X-Frame-Options header
        /// which is used to prevent ClickJacking. By default, the X-Frame-Options
        /// header is generated with the value SAMEORIGIN. If this setting is 'true',
        /// the X-Frame-Options header will not be generated for the response.
        /// </summary>
        public bool SuppressXFrameOptionsHeader { get; set; }

        #region Obsolete API
        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Name"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Specifies the name of the cookie that is used by the antiforgery system.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If an explicit name is not provided, the system will automatically generate a
        /// unique name that begins with <see cref="DefaultCookiePrefix"/>.
        /// </remarks>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Name) + ".")]
        public string CookieName { get => Cookie.Name; set => Cookie.Name = value; }

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
        public PathString? CookiePath { get => Cookie.Path; set => Cookie.Path = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Domain"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// The domain set on the cookie. By default its <c>null</c> which results in the "domain" attribute not being set.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Domain) + ".")]
        public string CookieDomain { get => Cookie.Domain; set => Cookie.Domain = value; }


        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. 
        /// The recommended alternative is to set <seealso cref="CookieBuilder.SecurePolicy"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// <c>true</c> is equivalent to <see cref="CookieSecurePolicy.Always"/>.
        /// <c>false</c> is equivalent to <see cref="CookieSecurePolicy.None"/>.
        /// </para>
        /// <para>
        /// Specifies whether SSL is required for the antiforgery system
        /// to operate. If this setting is 'true' and a non-SSL request
        /// comes into the system, all antiforgery APIs will fail.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is to set " + nameof(Cookie) + "." + nameof(CookieBuilder.SecurePolicy) + ".")]
        public bool RequireSsl
        {
            get => Cookie.SecurePolicy == CookieSecurePolicy.Always;
            set => Cookie.SecurePolicy = value ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        }
        #endregion
    }
}
