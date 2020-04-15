// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    /// <summary>
    /// Configuration options for <see cref="CookieAuthenticationOptions"/>.
    /// </summary>
    public class CookieAuthenticationOptions : AuthenticationSchemeOptions
    {
        private CookieBuilder _cookieBuilder = new RequestPathBaseCookieBuilder
        {
            // the default name is configured in PostConfigureCookieAuthenticationOptions

            // To support OAuth authentication, a lax mode is required, see https://github.com/aspnet/Security/issues/1231.
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            SecurePolicy = CookieSecurePolicy.SameAsRequest,
            IsEssential = true,
        };

        /// <summary>
        /// Create an instance of the options initialized with the default values
        /// </summary>
        public CookieAuthenticationOptions()
        {
            ExpireTimeSpan = TimeSpan.FromDays(14);
            ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            SlidingExpiration = true;
            Events = new CookieAuthenticationEvents();
        }

        /// <summary>
        /// <para>
        /// Determines the settings used to create the cookie.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Lax"/>.
        /// <see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.
        /// <see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.SameAsRequest"/>.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value for cookie <see cref="CookieBuilder.Name"/> is ".AspNetCore.Cookies".
        /// This value should be changed if you change the name of the <c>AuthenticationScheme</c>, especially if your
        /// system uses the cookie authentication handler multiple times.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.SameSite"/> determines if the browser should allow the cookie to be attached to same-site or cross-site requests.
        /// The default is <c>Lax</c>, which means the cookie is only allowed to be attached to cross-site requests using safe HTTP methods and same-site requests.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.HttpOnly"/> determines if the browser should allow the cookie to be accessed by client-side javascript.
        /// The default is true, which means the cookie will only be passed to http requests and is not made available to script on the page.
        /// </para>
        /// <para>
        /// <see cref="CookieBuilder.Expiration"/> is currently ignored. Use <see cref="ExpireTimeSpan"/> to control lifetime of cookie authentication.
        /// </para>
        /// </remarks>
        public CookieBuilder Cookie
        {
            get => _cookieBuilder;
            set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// If set this will be used by the CookieAuthenticationHandler for data protection.
        /// </summary>
        public IDataProtectionProvider DataProtectionProvider { get; set; }

        /// <summary>
        /// The SlidingExpiration is set to true to instruct the handler to re-issue a new cookie with a new
        /// expiration time any time it processes a request which is more than halfway through the expiration window.
        /// </summary>
        public bool SlidingExpiration { get; set; }

        /// <summary>
        /// The LoginPath property is used by the handler for the redirection target when handling ChallengeAsync.
        /// The current url which is added to the LoginPath as a query string parameter named by the ReturnUrlParameter.
        /// Once a request to the LoginPath grants a new SignIn identity, the ReturnUrlParameter value is used to redirect
        /// the browser back to the original url.
        /// </summary>
        public PathString LoginPath { get; set; }

        /// <summary>
        /// If the LogoutPath is provided the handler then a request to that path will redirect based on the ReturnUrlParameter.
        /// </summary>
        public PathString LogoutPath { get; set; }

        /// <summary>
        /// The AccessDeniedPath property is used by the handler for the redirection target when handling ForbidAsync.
        /// </summary>
        public PathString AccessDeniedPath { get; set; }

        /// <summary>
        /// The ReturnUrlParameter determines the name of the query string parameter which is appended by the handler
        /// when during a Challenge. This is also the query string parameter looked for when a request arrives on the
        /// login path or logout path, in order to return to the original url after the action is performed.
        /// </summary>
        public string ReturnUrlParameter { get; set; }

        /// <summary>
        /// The Provider may be assigned to an instance of an object created by the application at startup time. The handler
        /// calls methods on the provider which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        public new CookieAuthenticationEvents Events
        {
            get => (CookieAuthenticationEvents)base.Events;
            set => base.Events = value;
        }

        /// <summary>
        /// The TicketDataFormat is used to protect and unprotect the identity and other properties which are stored in the
        /// cookie value. If not provided one will be created using <see cref="DataProtectionProvider"/>.
        /// </summary>
        public ISecureDataFormat<AuthenticationTicket> TicketDataFormat { get; set; }

        /// <summary>
        /// The component used to get cookies from the request or set them on the response.
        ///
        /// ChunkingCookieManager will be used by default.
        /// </summary>
        public ICookieManager CookieManager { get; set; }

        /// <summary>
        /// An optional container in which to store the identity across requests. When used, only a session identifier is sent
        /// to the client. This can be used to mitigate potential problems with very large identities.
        /// </summary>
        public ITicketStore SessionStore { get; set; }

        /// <summary>
        /// <para>
        /// Controls how much time the authentication ticket stored in the cookie will remain valid from the point it is created
        /// The expiration information is stored in the protected cookie ticket. Because of that an expired cookie will be ignored
        /// even if it is passed to the server after the browser should have purged it.
        /// </para>
        /// <para>
        /// This is separate from the value of <see cref="CookieOptions.Expires"/>, which specifies
        /// how long the browser will keep the cookie.
        /// </para>
        /// </summary>
        public TimeSpan ExpireTimeSpan { get; set; }
    }
}
