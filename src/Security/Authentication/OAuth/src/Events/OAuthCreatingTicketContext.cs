// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class OAuthCreatingTicketContext : ResultContext<OAuthOptions>
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthCreatingTicketContext"/>.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="options">The options used by the authentication middleware.</param>
        /// <param name="backchannel">The HTTP client used by the authentication middleware</param>
        /// <param name="tokens">The tokens returned from the token endpoint.</param>
        /// <param name="user">The JSON-serialized user.</param>
        public OAuthCreatingTicketContext(
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            HttpContext context,
            AuthenticationScheme scheme,
            OAuthOptions options,
            HttpClient backchannel,
            OAuthTokenResponse tokens,
            JsonElement user)
            : base(context, scheme, options)
        {
            if (backchannel == null)
            {
                throw new ArgumentNullException(nameof(backchannel));
            }

            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            TokenResponse = tokens;
            Backchannel = backchannel;
            User = user;
            Principal = principal;
            Properties = properties;
        }

        /// <summary>
        /// Gets the JSON-serialized user or an empty
        /// <see cref="JsonElement"/> if it is not available.
        /// </summary>
        public JsonElement User { get; }

        /// <summary>
        /// Gets the token response returned by the authentication service.
        /// </summary>
        public OAuthTokenResponse TokenResponse { get; }

        /// <summary>
        /// Gets the access token provided by the authentication service.
        /// </summary>
        public string AccessToken => TokenResponse.AccessToken;

        /// <summary>
        /// Gets the access token type provided by the authentication service.
        /// </summary>
        public string TokenType => TokenResponse.TokenType;

        /// <summary>
        /// Gets the refresh token provided by the authentication service.
        /// </summary>
        public string RefreshToken => TokenResponse.RefreshToken;

        /// <summary>
        /// Gets the access token expiration time.
        /// </summary>
        public TimeSpan? ExpiresIn
        {
            get
            {
                int value;
                if (int.TryParse(TokenResponse.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    return TimeSpan.FromSeconds(value);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the backchannel used to communicate with the provider.
        /// </summary>
        public HttpClient Backchannel { get; }

        /// <summary>
        /// Gets the main identity exposed by the authentication ticket.
        /// This property returns <c>null</c> when the ticket is <c>null</c>.
        /// </summary>
        public ClaimsIdentity Identity => Principal?.Identity as ClaimsIdentity;

        public void RunClaimActions() => RunClaimActions(User);

        public void RunClaimActions(JsonElement userData)
        {
            foreach (var action in Options.ClaimActions)
            {
                action.Run(userData, Identity, Options.ClaimsIssuer ?? Scheme.Name);
            }
        }
    }
}
