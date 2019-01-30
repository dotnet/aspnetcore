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
