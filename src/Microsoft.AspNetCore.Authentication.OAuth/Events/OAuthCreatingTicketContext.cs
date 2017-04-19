// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class OAuthCreatingTicketContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthCreatingTicketContext"/>.
        /// </summary>
        /// <param name="ticket">The <see cref="AuthenticationTicket"/>.</param>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="options">The options used by the authentication middleware.</param>
        /// <param name="backchannel">The HTTP client used by the authentication middleware</param>
        /// <param name="tokens">The tokens returned from the token endpoint.</param>
        public OAuthCreatingTicketContext(
            AuthenticationTicket ticket,
            HttpContext context,
            AuthenticationScheme scheme,
            OAuthOptions options,
            HttpClient backchannel,
            OAuthTokenResponse tokens)
            : this(ticket, context, scheme, options, backchannel, tokens, user: new JObject())
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OAuthCreatingTicketContext"/>.
        /// </summary>
        /// <param name="ticket">The <see cref="AuthenticationTicket"/>.</param>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="options">The options used by the authentication middleware.</param>
        /// <param name="backchannel">The HTTP client used by the authentication middleware</param>
        /// <param name="tokens">The tokens returned from the token endpoint.</param>
        /// <param name="user">The JSON-serialized user.</param>
        public OAuthCreatingTicketContext(
            AuthenticationTicket ticket,
            HttpContext context,
            AuthenticationScheme scheme,
            OAuthOptions options,
            HttpClient backchannel,
            OAuthTokenResponse tokens,
            JObject user)
            : base(context, scheme.Name, ticket.Properties)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (backchannel == null)
            {
                throw new ArgumentNullException(nameof(backchannel));
            }

            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (scheme == null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }

            TokenResponse = tokens;
            Backchannel = backchannel;
            User = user;
            Options = options;
            Scheme = scheme;
            Ticket = ticket;
        }

        public OAuthOptions Options { get; }

        public AuthenticationScheme Scheme { get; }

        /// <summary>
        /// Gets the JSON-serialized user or an empty
        /// <see cref="JObject"/> if it is not available.
        /// </summary>
        public JObject User { get; }

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
        /// The <see cref="AuthenticationTicket"/> that will be created.
        /// </summary>
        public AuthenticationTicket Ticket { get; set; }

        /// <summary>
        /// Gets the main identity exposed by <see cref="Ticket"/>.
        /// This property returns <c>null</c> when <see cref="Ticket"/> is <c>null</c>.
        /// </summary>
        public ClaimsIdentity Identity => Ticket?.Principal.Identity as ClaimsIdentity;

        public void RunClaimActions()
        {
            RunClaimActions(User);
        }

        public void RunClaimActions(JObject userData)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }

            foreach (var action in Options.ClaimActions)
            {
                action.Run(userData, Identity, Options.ClaimsIssuer);
            }
        }
    }
}