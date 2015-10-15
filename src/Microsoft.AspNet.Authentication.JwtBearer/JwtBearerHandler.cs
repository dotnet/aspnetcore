// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.JwtBearer
{
    internal class JwtBearerHandler : AuthenticationHandler<JwtBearerOptions>
    {
        private OpenIdConnectConfiguration _configuration;

        /// <summary>
        /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string token = null;
            try
            {
                // Give application opportunity to find from a different location, adjust, or reject token
                var receivingTokenContext = new ReceivingTokenContext(Context, Options);

                // event can set the token
                await Options.Events.ReceivingToken(receivingTokenContext);
                if (receivingTokenContext.HandledResponse)
                {
                    return AuthenticateResult.Success(receivingTokenContext.AuthenticationTicket);
                }

                if (receivingTokenContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }

                // If application retrieved token from somewhere else, use that.
                token = receivingTokenContext.Token;

                if (string.IsNullOrEmpty(token))
                {
                    string authorization = Request.Headers["Authorization"];

                    // If no authorization header found, nothing to process further
                    if (string.IsNullOrEmpty(authorization))
                    {
                        return AuthenticateResult.Failed("No authorization header.");
                    }

                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authorization.Substring("Bearer ".Length).Trim();
                    }

                    // If no token found, no further work possible
                    if (string.IsNullOrEmpty(token))
                    {
                        return AuthenticateResult.Failed("No bearer token.");
                    }
                }

                // notify user token was received
                var receivedTokenContext = new ReceivedTokenContext(Context, Options)
                {
                    Token = token,
                };

                await Options.Events.ReceivedToken(receivedTokenContext);
                if (receivedTokenContext.HandledResponse)
                {
                    return AuthenticateResult.Success(receivedTokenContext.AuthenticationTicket);
                }

                if (receivedTokenContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }

                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                var validationParameters = Options.TokenValidationParameters.Clone();
                if (_configuration != null)
                {
                    if (validationParameters.ValidIssuer == null && !string.IsNullOrEmpty(_configuration.Issuer))
                    {
                        validationParameters.ValidIssuer = _configuration.Issuer;
                    }
                    else
                    {
                        var issuers = new[] { _configuration.Issuer };
                        validationParameters.ValidIssuers = (validationParameters.ValidIssuers == null ? issuers : validationParameters.ValidIssuers.Concat(issuers));
                    }

                    validationParameters.IssuerSigningKeys = (validationParameters.IssuerSigningKeys == null ? _configuration.SigningKeys : validationParameters.IssuerSigningKeys.Concat(_configuration.SigningKeys));
                }

                SecurityToken validatedToken;
                foreach (var validator in Options.SecurityTokenValidators)
                {
                    if (validator.CanReadToken(token))
                    {
                        var principal = validator.ValidateToken(token, validationParameters, out validatedToken);
                        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), Options.AuthenticationScheme);
                        var validatedTokenContext = new ValidatedTokenContext(Context, Options)
                        {
                            AuthenticationTicket = ticket
                        };

                        await Options.Events.ValidatedToken(validatedTokenContext);
                        if (validatedTokenContext.HandledResponse)
                        {
                            return AuthenticateResult.Success(validatedTokenContext.AuthenticationTicket);
                        }

                        if (validatedTokenContext.Skipped)
                        {
                            return AuthenticateResult.Success(ticket: null);
                        }

                        return AuthenticateResult.Success(ticket);
                    }
                }

                // REVIEW: this maybe return an error instead?
                throw new InvalidOperationException("No SecurityTokenValidator available for token: " + token ?? "null");
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception occurred while processing message", ex);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
                if (Options.RefreshOnIssuerKeyNotFound && ex.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    Options.ConfigurationManager.RequestRefresh();
                }

                var authenticationFailedContext = new AuthenticationFailedContext(Context, Options)
                {
                    Exception = ex
                };

                await Options.Events.AuthenticationFailed(authenticationFailedContext);
                if (authenticationFailedContext.HandledResponse)
                {
                    return AuthenticateResult.Success(authenticationFailedContext.AuthenticationTicket);
                }
                if (authenticationFailedContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }

                throw;
            }
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            Response.StatusCode = 401;
            await Options.Events.Challenge(new JwtBearerChallengeContext(Context, Options));
            return false;
        }

        protected override Task HandleSignOutAsync(SignOutContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task HandleSignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }
    }
}
