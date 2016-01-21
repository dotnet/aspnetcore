// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
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
                    return AuthenticateResult.Success(receivingTokenContext.Ticket);
                }
                if (receivingTokenContext.Skipped)
                {
                    return AuthenticateResult.Skip();
                }

                // If application retrieved token from somewhere else, use that.
                token = receivingTokenContext.Token;

                if (string.IsNullOrEmpty(token))
                {
                    string authorization = Request.Headers["Authorization"];

                    // If no authorization header found, nothing to process further
                    if (string.IsNullOrEmpty(authorization))
                    {
                        return AuthenticateResult.Skip();
                    }

                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authorization.Substring("Bearer ".Length).Trim();
                    }

                    // If no token found, no further work possible
                    if (string.IsNullOrEmpty(token))
                    {
                        return AuthenticateResult.Skip();
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
                    return AuthenticateResult.Success(receivedTokenContext.Ticket);
                }
                if (receivedTokenContext.Skipped)
                {
                    return AuthenticateResult.Skip();
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

                List<Exception> validationFailures = null;
                SecurityToken validatedToken;
                foreach (var validator in Options.SecurityTokenValidators)
                {
                    if (validator.CanReadToken(token))
                    {
                        ClaimsPrincipal principal;
                        try
                        {
                            principal = validator.ValidateToken(token, validationParameters, out validatedToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogInformation(0, ex, "Failed to validate the token: " + token);

                            // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
                            if (Options.RefreshOnIssuerKeyNotFound && ex.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                            {
                                Options.ConfigurationManager.RequestRefresh();
                            }

                            if (validationFailures == null)
                            {
                                validationFailures = new List<Exception>(1);
                            }
                            validationFailures.Add(ex);
                            continue;
                        }

                        Logger.LogInformation("Successfully validated the token");

                        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), Options.AuthenticationScheme);
                        var validatedTokenContext = new ValidatedTokenContext(Context, Options)
                        {
                            Ticket = ticket
                        };

                        await Options.Events.ValidatedToken(validatedTokenContext);
                        if (validatedTokenContext.HandledResponse)
                        {
                            return AuthenticateResult.Success(validatedTokenContext.Ticket);
                        }
                        if (validatedTokenContext.Skipped)
                        {
                            return AuthenticateResult.Skip();
                        }

                        return AuthenticateResult.Success(ticket);
                    }
                }

                if (validationFailures != null)
                {
                    var authenticationFailedContext = new AuthenticationFailedContext(Context, Options)
                    {
                        Exception = (validationFailures.Count == 1) ? validationFailures[0] : new AggregateException(validationFailures)
                    };

                    await Options.Events.AuthenticationFailed(authenticationFailedContext);
                    if (authenticationFailedContext.HandledResponse)
                    {
                        return AuthenticateResult.Success(authenticationFailedContext.Ticket);
                    }
                    if (authenticationFailedContext.Skipped)
                    {
                        return AuthenticateResult.Skip();
                    }

                    return AuthenticateResult.Fail(authenticationFailedContext.Exception);
                }

                return AuthenticateResult.Fail("No SecurityTokenValidator available for token: " + token ?? "[null]");
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Exception occurred while processing message");

                var authenticationFailedContext = new AuthenticationFailedContext(Context, Options)
                {
                    Exception = ex
                };

                await Options.Events.AuthenticationFailed(authenticationFailedContext);
                if (authenticationFailedContext.HandledResponse)
                {
                    return AuthenticateResult.Success(authenticationFailedContext.Ticket);
                }
                if (authenticationFailedContext.Skipped)
                {
                    return AuthenticateResult.Skip();
                }

                throw;
            }
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            var eventContext = new JwtBearerChallengeContext(Context, Options, new AuthenticationProperties(context.Properties));
            await Options.Events.Challenge(eventContext);
            if (eventContext.HandledResponse)
            {
                return true;
            }
            if (eventContext.Skipped)
            {
                return false;
            }

            Response.StatusCode = 401;
            Response.Headers.Append(HeaderNames.WWWAuthenticate, Options.Challenge);

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
