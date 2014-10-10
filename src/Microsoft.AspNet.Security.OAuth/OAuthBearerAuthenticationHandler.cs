// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Security.OAuth
{
    internal class OAuthBearerAuthenticationHandler : AuthenticationHandler<OAuthBearerAuthenticationOptions>
    {
        private readonly ILogger _logger;
        private readonly string _challenge;

        public OAuthBearerAuthenticationHandler(ILogger logger, string challenge)
        {
            _logger = logger;
            _challenge = challenge;
        }

        protected override AuthenticationTicket AuthenticateCore()
        {
            return AuthenticateCoreAsync().GetAwaiter().GetResult();
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            try
            {
                // Find token in default location
                string requestToken = null;
                string authorization = Request.Headers.Get("Authorization");
                if (!string.IsNullOrEmpty(authorization))
                {
                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        requestToken = authorization.Substring("Bearer ".Length).Trim();
                    }
                }

                // Give application opportunity to find from a different location, adjust, or reject token
                var requestTokenContext = new OAuthRequestTokenContext(Context, requestToken);
                await Options.Notifications.RequestToken(requestTokenContext);

                // If no token found, no further work possible
                if (string.IsNullOrEmpty(requestTokenContext.Token))
                {
                    return null;
                }

                // Call provider to process the token into data
                var tokenReceiveContext = new AuthenticationTokenReceiveContext(
                    Context,
                    Options.AccessTokenFormat,
                    requestTokenContext.Token);

                await Options.AccessTokenProvider.ReceiveAsync(tokenReceiveContext);
                if (tokenReceiveContext.Ticket == null)
                {
                    tokenReceiveContext.DeserializeTicket(tokenReceiveContext.Token);
                }

                AuthenticationTicket ticket = tokenReceiveContext.Ticket;
                if (ticket == null)
                {
                    _logger.WriteWarning("invalid bearer token received");
                    return null;
                }

                // Validate expiration time if present
                DateTimeOffset currentUtc = Options.SystemClock.UtcNow;

                if (ticket.Properties.ExpiresUtc.HasValue &&
                    ticket.Properties.ExpiresUtc.Value < currentUtc)
                {
                    _logger.WriteWarning("expired bearer token received");
                    return null;
                }

                // Give application final opportunity to override results
                var context = new OAuthValidateIdentityContext(Context, Options, ticket);
                if (ticket != null &&
                    ticket.Identity != null &&
                    ticket.Identity.IsAuthenticated)
                {
                    // bearer token with identity starts validated
                    context.Validated();
                }

                await Options.Notifications.ValidateIdentity(context);
                if (!context.IsValidated)
                {
                    return null;
                }

                // resulting identity values go back to caller
                return context.Ticket;
            }
            catch (Exception ex)
            {
                _logger.WriteError("Authentication failed", ex);
                return null;
            }
        }

        protected override void ApplyResponseChallenge()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            if (ChallengeContext != null)
            {
                OAuthChallengeContext challengeContext = new OAuthChallengeContext(Context, _challenge);
                Options.Notifications.ApplyChallenge(challengeContext);
            }
        }

        protected override void ApplyResponseGrant()
        {
            // N/A
        }
    }
}
