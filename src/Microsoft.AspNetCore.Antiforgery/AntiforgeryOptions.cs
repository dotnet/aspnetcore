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

        private string _cookieName;
        private string _formFieldName = AntiforgeryTokenFieldName;

        /// <summary>
        /// The default cookie prefix, which is ".AspNetCore.Antiforgery.".
        /// </summary>
        public static readonly string DefaultCookiePrefix = ".AspNetCore.Antiforgery.";

        /// <summary>
        /// Specifies the name of the cookie that is used by the antiforgery system.
        /// </summary>
        /// <remarks>
        /// If an explicit name is not provided, the system will automatically generate a
        /// unique name that begins with <see cref="DefaultCookiePrefix"/>.
        /// </remarks>
        public string CookieName
        {
            get
            {
                return _cookieName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _cookieName = value;
            }
        }

        /// <summary>
        /// This is obsolete and will be removed in a future version.
        /// The recommended alternative is to use ConfigureCookieOptions.
        /// The path set on the cookie. If set to <c>null</c>, the "path" attribute on the cookie is set to the current
        /// request's <see cref="HttpRequest.PathBase"/> value. If the value of <see cref="HttpRequest.PathBase"/> is
        /// <c>null</c> or empty, then the "path" attribute is set to the value of <see cref="CookieOptions.Path"/>.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use ConfigureCookieOptions.")]
        public PathString? CookiePath { get; set; }

        /// <summary>
        /// This is obsolete and will be removed in a future version.
        /// The recommended alternative is to use ConfigureCookieOptions.
        /// The domain set on the cookie. By default its <c>null</c> which results in the "domain" attribute not being set.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. The recommended alternative is to use ConfigureCookieOptions.")]
        public string CookieDomain { get; set; }

        /// <summary>
        /// Configures the <see cref="CookieOptions"/> of the antiforgery cookies. Without additional configuration, the 
        /// default values antiforgery cookie options are true for <see cref="CookieOptions.HttpOnly"/>, null for
        /// <see cref="CookieOptions.Domain"/> and <see cref="SameSiteMode.Strict"/> for <see cref="CookieOptions.SameSite"/>.
        /// </summary>
        public Action<HttpContext, CookieOptions> ConfigureCookieOptions { get; set; }

        /// <summary>
        /// Specifies the name of the antiforgery token field that is used by the antiforgery system.
        /// </summary>
        public string FormFieldName
        {
            get
            {
                return _formFieldName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _formFieldName = value;
            }
        }

        /// <summary>
        /// Specifies the name of the header value that is used by the antiforgery system. If <c>null</c> then
        /// antiforgery validation will only consider form data.
        /// </summary>
        public string HeaderName { get; set; } = AntiforgeryTokenHeaderName;

        /// <summary>
        /// Specifies whether SSL is required for the antiforgery system
        /// to operate. If this setting is 'true' and a non-SSL request
        /// comes into the system, all antiforgery APIs will fail.
        /// </summary>
        public bool RequireSsl { get; set; }

        /// <summary>
        /// Specifies whether to suppress the generation of X-Frame-Options header
        /// which is used to prevent ClickJacking. By default, the X-Frame-Options
        /// header is generated with the value SAMEORIGIN. If this setting is 'true',
        /// the X-Frame-Options header will not be generated for the response.
        /// </summary>
        public bool SuppressXFrameOptionsHeader { get; set; }
    }
}