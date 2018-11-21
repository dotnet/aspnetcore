// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents the session state options for the application.
    /// </summary>
    public class SessionOptions
    {
        private CookieBuilder _cookieBuilder = new SessionCookieBuilder();

        /// <summary>
        /// Determines the settings used to create the cookie.
        /// <para>
        /// <see cref="CookieBuilder.Name"/> defaults to <see cref="SessionDefaults.CookieName"/>.
        /// <see cref="CookieBuilder.Path"/> defaults to <see cref="SessionDefaults.CookiePath"/>.
        /// <see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Lax"/>.
        /// <see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>
        /// <see cref="CookieBuilder.IsEssential"/> defaults to <c>false</c>
        /// </para>
        /// </summary>
        public CookieBuilder Cookie
        {
            get => _cookieBuilder;
            set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The IdleTimeout indicates how long the session can be idle before its contents are abandoned. Each session access
        /// resets the timeout. Note this only applies to the content of the session, not the cookie.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// The maximim amount of time allowed to load a session from the store or to commit it back to the store.
        /// Note this may only apply to asynchronous operations. This timeout can be disabled using <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </summary>
        public TimeSpan IOTimeout { get; set; } = TimeSpan.FromMinutes(1);

        #region Obsolete API
        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Name"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Determines the cookie name used to persist the session ID.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Name) + ".")]
        public string CookieName { get => Cookie.Name; set => Cookie.Name = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Domain"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Determines the domain used to create the cookie. Is not provided by default.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Domain) + ".")]
        public string CookieDomain { get => Cookie.Domain; set => Cookie.Domain = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.Path"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Determines the path used to create the cookie.
        /// Defaults to <see cref="SessionDefaults.CookiePath"/>.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.Path) + ".")]
        public string CookiePath { get => Cookie.Path; set => Cookie.Path = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.HttpOnly"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Determines if the browser should allow the cookie to be accessed by client-side JavaScript. The
        /// default is true, which means the cookie will only be passed to HTTP requests and is not made available
        /// to script on the page.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.HttpOnly) + ".")]
        public bool CookieHttpOnly { get => Cookie.HttpOnly; set => Cookie.HttpOnly = value; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version. The recommended alternative is <seealso cref="CookieBuilder.SecurePolicy"/> on <see cref="Cookie"/>.
        /// </para>
        /// <para>
        /// Determines if the cookie should only be transmitted on HTTPS requests.
        /// </para>
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. The recommended alternative is " + nameof(Cookie) + "." + nameof(CookieBuilder.SecurePolicy) + ".")]
        public CookieSecurePolicy CookieSecure { get => Cookie.SecurePolicy; set => Cookie.SecurePolicy = value; }
        #endregion

        private class SessionCookieBuilder : CookieBuilder
        {
            public SessionCookieBuilder()
            {
                Name = SessionDefaults.CookieName;
                Path = SessionDefaults.CookiePath;
                SecurePolicy = CookieSecurePolicy.None;
                SameSite = SameSiteMode.Lax;
                HttpOnly = true;
                // Session is considered non-essential as it's designed for ephemeral data.
                IsEssential = false;
            }

            public override TimeSpan? Expiration
            {
                get => null;
                set => throw new InvalidOperationException(nameof(Expiration) + " cannot be set for the cookie defined by " + nameof(SessionOptions));
            }
        }
    }
}
