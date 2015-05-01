// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.Notifications;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class OAuthAuthenticatedContext : BaseContext<OAuthAuthenticationOptions>
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthAuthenticatedContext"/>.
        /// </summary>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="user">The JSON-serialized user.</param>
        /// <param name="tokens">The tokens returned from the token endpoint.</param>
        public OAuthAuthenticatedContext(HttpContext context, OAuthAuthenticationOptions options, JObject user,
            TokenResponse tokens)
            : base(context, options)
        {
            User = user;
            AccessToken = tokens.AccessToken;
            TokenType = tokens.TokenType;
            RefreshToken = tokens.RefreshToken;

            int expiresInValue;
            if (Int32.TryParse(tokens.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out expiresInValue))
            {
                ExpiresIn = TimeSpan.FromSeconds(expiresInValue);
            }
        }

        /// <summary>
        /// Gets the JSON-serialized user.
        /// </summary>
        public JObject User { get; protected set; }

        /// <summary>
        /// Gets the access token provided by the authentication service.
        /// </summary>
        public string AccessToken { get; protected set; }

        /// <summary>
        /// Gets the access token type provided by the authentication service.
        /// </summary>
        public string TokenType { get; protected set; }

        /// <summary>
        /// Gets the refresh token provided by the authentication service.
        /// </summary>
        public string RefreshToken { get; protected set; }

        /// <summary>
        /// Gets the access token expiration time.
        /// </summary>
        public TimeSpan? ExpiresIn { get; protected set; }

        /// <summary>
        /// Gets the <see cref="ClaimsIdentity"/> representing the user.
        /// </summary>
        public ClaimsPrincipal Principal { get; set; }

        /// <summary>
        /// Gets or sets a property bag for common authentication properties.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }
    }
}