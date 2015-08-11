// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Framework.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OAuthBearer
{
    public class OAuthBearerAuthenticationHandler : AuthenticationHandler<OAuthBearerAuthenticationOptions>
    {
        private OpenIdConnectConfiguration _configuration;

        /// <summary>
        /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticationTicket> HandleAuthenticateAsync()
        {
            string token = null;
            try
            {
                // Give application opportunity to find from a different location, adjust, or reject token
                var messageReceivedNotification =
                    new MessageReceivedNotification<HttpContext, OAuthBearerAuthenticationOptions>(Context, Options)
                    {
                        ProtocolMessage = Context,
                    };

                // notification can set the token
                await Options.Notifications.MessageReceived(messageReceivedNotification);
                if (messageReceivedNotification.HandledResponse)
                {
                    return messageReceivedNotification.AuthenticationTicket;
                }

                if (messageReceivedNotification.Skipped)
                {
                    return null;
                }

                // If application retrieved token from somewhere else, use that.
                token = messageReceivedNotification.Token;

                if (string.IsNullOrEmpty(token))
                {
                    var authorization = Request.Headers.Get("Authorization");

                    // If no authorization header found, nothing to process further
                    if (string.IsNullOrEmpty(authorization))
                    {
                        return null;
                    }

                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authorization.Substring("Bearer ".Length).Trim();
                    }

                    // If no token found, no further work possible
                    if (string.IsNullOrEmpty(token))
                    {
                        return null;
                    }
                }

                // notify user token was received
                var securityTokenReceivedNotification =
                    new SecurityTokenReceivedNotification<HttpContext, OAuthBearerAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = Context,
                    SecurityToken = token,
                };

                await Options.Notifications.SecurityTokenReceived(securityTokenReceivedNotification);
                if (securityTokenReceivedNotification.HandledResponse)
                {
                    return securityTokenReceivedNotification.AuthenticationTicket;
                }

                if (securityTokenReceivedNotification.Skipped)
                {
                    return null;
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
                        var securityTokenValidatedNotification = new SecurityTokenValidatedNotification<HttpContext, OAuthBearerAuthenticationOptions>(Context, Options)
                        {
                            ProtocolMessage = Context,
                            AuthenticationTicket = ticket
                        };

                        await Options.Notifications.SecurityTokenValidated(securityTokenValidatedNotification);
                        if (securityTokenValidatedNotification.HandledResponse)
                        {
                            return securityTokenValidatedNotification.AuthenticationTicket;
                        }

                        if (securityTokenValidatedNotification.Skipped)
                        {
                            return null;
                        }

                        return ticket;
                    }
                }

                throw new InvalidOperationException("No SecurityTokenValidator available for token: " + token ?? "null");
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception occurred while processing message", ex);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
                if (Options.RefreshOnIssuerKeyNotFound && ex.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    Options.ConfigurationManager.RequestRefresh();
                }

                var authenticationFailedNotification =
                    new AuthenticationFailedNotification<HttpContext, OAuthBearerAuthenticationOptions>(Context, Options)
                    {
                        ProtocolMessage = Context,
                        Exception = ex
                    };

                await Options.Notifications.AuthenticationFailed(authenticationFailedNotification);
                if (authenticationFailedNotification.HandledResponse)
                {
                    return authenticationFailedNotification.AuthenticationTicket;
                }

                if (authenticationFailedNotification.Skipped)
                {
                    return null;
                }

                throw;
            }
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            Response.StatusCode = 401;
            await Options.Notifications.ApplyChallenge(new AuthenticationChallengeNotification<OAuthBearerAuthenticationOptions>(Context, Options));
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
