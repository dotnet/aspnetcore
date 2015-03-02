// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Notifications;
using Microsoft.Framework.Logging;
using Microsoft.IdentityModel.Protocols;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// A per-request authentication handler for the OpenIdConnectAuthenticationMiddleware.
    /// </summary>
    public class OpenIdConnectAuthenticationHandler : AuthenticationHandler<OpenIdConnectAuthenticationOptions>
    {
        private const string NonceProperty = "N";
        private const string UriSchemeDelimiter = "://";
        private readonly ILogger _logger;
        private OpenIdConnectConfiguration _configuration;

        /// <summary>
        /// Creates a new OpenIdConnectAuthenticationHandler
        /// </summary>
        /// <param name="logger"></param>
        public OpenIdConnectAuthenticationHandler(ILogger logger)
        {
            _logger = logger;
        }

        private string CurrentUri
        {
            get
            {
                return Request.Scheme +
                       UriSchemeDelimiter +
                       Request.Host +
                       Request.PathBase +
                       Request.Path +
                       Request.QueryString;
            }
        }

        protected override void ApplyResponseGrant()
        {
            ApplyResponseGrantAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles Signout
        /// </summary>
        /// <returns></returns>
        protected override async Task ApplyResponseGrantAsync()
        {
            var signout = SignOutContext;
            if (signout != null)
            {
                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                OpenIdConnectMessage openIdConnectMessage = new OpenIdConnectMessage()
                {
                    IssuerAddress = _configuration == null ? string.Empty : (_configuration.EndSessionEndpoint ?? string.Empty),
                    RequestType = OpenIdConnectRequestType.LogoutRequest,
                };

                // Set End_Session_Endpoint in order:
                // 1. properties.Redirect
                // 2. Options.Wreply
                AuthenticationProperties properties = new AuthenticationProperties();
                if (properties != null && !string.IsNullOrEmpty(properties.RedirectUri))
                {
                    openIdConnectMessage.PostLogoutRedirectUri = properties.RedirectUri;
                }
                else if (!string.IsNullOrWhiteSpace(Options.PostLogoutRedirectUri))
                {
                    openIdConnectMessage.PostLogoutRedirectUri = Options.PostLogoutRedirectUri;
                }

                var notification = new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = openIdConnectMessage
                };

                await Options.Notifications.RedirectToIdentityProvider(notification);

                if (!notification.HandledResponse)
                {
                    string redirectUri = notification.ProtocolMessage.CreateLogoutRequestUrl();
                    if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                    {
                        _logger.WriteWarning("The logout redirect URI is malformed: {0}", (redirectUri ?? "null"));
                    }

                    Response.Redirect(redirectUri);
                }
            }
        }

        protected override void ApplyResponseChallenge()
        {
            ApplyResponseChallengeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Responds to a 401 Challenge. Sends an OpenIdConnect message to the 'identity authority' to obtain an identity.
        /// </summary>
        /// <returns></returns>
        protected override async Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            // Only redirect on challenges
            if (ChallengeContext == null)
            {
                return;
            }

            // order for redirect_uri
            // 1. challenge.Properties.RedirectUri
            // 2. CurrentUri
            AuthenticationProperties properties;
            if (ChallengeContext == null)
            {
                properties = new AuthenticationProperties();
            }
            else
            {
                properties = new AuthenticationProperties(ChallengeContext.Properties);
            }

            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }

            // this value will be passed to the AuthorizationCodeReceivedNotification
            if (!string.IsNullOrWhiteSpace(Options.RedirectUri))
            {
                properties.Dictionary.Add(OpenIdConnectAuthenticationDefaults.RedirectUriUsedForCodeKey, Options.RedirectUri);
            }

            if (_configuration == null && Options.ConfigurationManager != null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            OpenIdConnectMessage openIdConnectMessage = new OpenIdConnectMessage
            {
                ClientId = Options.ClientId,
                IssuerAddress = _configuration == null ? string.Empty : (_configuration.AuthorizationEndpoint ?? string.Empty),
                RedirectUri = Options.RedirectUri,
                RequestType = OpenIdConnectRequestType.AuthenticationRequest,
                Resource = Options.Resource,
                ResponseMode = OpenIdConnectResponseModes.FormPost,
                ResponseType = Options.ResponseType,
                Scope = Options.Scope,
                State = OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey + "=" + Uri.EscapeDataString(Options.StateDataFormat.Protect(properties))
            };

            if (Options.ProtocolValidator.RequireNonce)
            {
                openIdConnectMessage.Nonce = Options.ProtocolValidator.GenerateNonce();
                if (Options.NonceCache != null)
                {
                    Options.NonceCache.AddNonce(openIdConnectMessage.Nonce);
                }
                else
                {
                    RememberNonce(openIdConnectMessage.Nonce);
                }
            }

            var notification = new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
            {
                ProtocolMessage = openIdConnectMessage
            };

            await Options.Notifications.RedirectToIdentityProvider(notification);
            if (!notification.HandledResponse)
            {
                string redirectUri = notification.ProtocolMessage.CreateAuthenticationRequestUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    _logger.WriteWarning("Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute) returned 'false', redirectUri is: {0}", (redirectUri ?? "null"));
                }

                Response.Redirect(redirectUri);
            }
        }

        protected override AuthenticationTicket AuthenticateCore()
        {
            return AuthenticateCoreAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Invoked to process incoming OpenIdConnect messages.
        /// </summary>
        /// <returns>An <see cref="AuthenticationTicket"/> if successful.</returns>
        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            // Allow login to be constrained to a specific path. Need to make this runtime configurable.
            if (Options.CallbackPath.HasValue && Options.CallbackPath != (Request.PathBase + Request.Path))
            {
                return null;
            }

            OpenIdConnectMessage openIdConnectMessage = null;

            // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
            if (string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
              && !string.IsNullOrWhiteSpace(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                IFormCollection form = await Request.ReadFormAsync();
                Request.Body.Seek(0, SeekOrigin.Begin);

                openIdConnectMessage = new OpenIdConnectMessage(form);
            }

            if (openIdConnectMessage == null)
            {
                return null;
            }

            try
            {
                var messageReceivedNotification = new MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = openIdConnectMessage
                };

                await Options.Notifications.MessageReceived(messageReceivedNotification);
                if (messageReceivedNotification.HandledResponse)
                {
                    return messageReceivedNotification.AuthenticationTicket;
                }

                if (messageReceivedNotification.Skipped)
                {
                    return null;
                }

                // runtime always adds state, if we don't find it OR we failed to 'unprotect' it this is not a message we
                // should process.
                AuthenticationProperties properties = GetPropertiesFromState(openIdConnectMessage.State);
                if (properties == null)
                {
                    _logger.WriteWarning("The state field is missing or invalid.");
                    return null;
                }

                // devs will need to hook AuthenticationFailedNotification to avoid having 'raw' runtime errors displayed to users.
                if (!string.IsNullOrWhiteSpace(openIdConnectMessage.Error))
                {
                    throw new OpenIdConnectProtocolException(
                        string.Format(CultureInfo.InvariantCulture,
                                      openIdConnectMessage.Error,
                                      Resources.Exception_OpenIdConnectMessageError, openIdConnectMessage.ErrorDescription ?? string.Empty, openIdConnectMessage.ErrorUri ?? string.Empty));
                }

                // code is only accepted with id_token, in this version, hence check for code is inside this if
                // OpenIdConnect protocol allows a Code to be received without the id_token
                if (string.IsNullOrWhiteSpace(openIdConnectMessage.IdToken))
                {
                    _logger.WriteWarning("The id_token is missing.");
                    return null;
                }

                var securityTokenReceivedNotification = new SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = openIdConnectMessage
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

                // Copy and augment to avoid cross request race conditions for updated configurations.
                TokenValidationParameters validationParameters = Options.TokenValidationParameters.Clone();
                if (_configuration != null)
                {
                    if (string.IsNullOrWhiteSpace(validationParameters.ValidIssuer))
                    {
                        validationParameters.ValidIssuer = _configuration.Issuer;
                    }
                    else if (!string.IsNullOrWhiteSpace(_configuration.Issuer))
                    {
                        validationParameters.ValidIssuers = (validationParameters.ValidIssuers == null ? new[] { _configuration.Issuer } : validationParameters.ValidIssuers.Concat(new[] { _configuration.Issuer }));
                    }

                    validationParameters.IssuerSigningKeys = (validationParameters.IssuerSigningKeys == null ? _configuration.SigningKeys : validationParameters.IssuerSigningKeys.Concat(_configuration.SigningKeys));
                }

                AuthenticationTicket ticket;
                SecurityToken validatedToken = null;
                ClaimsPrincipal principal = null;
                JwtSecurityToken jwt = null;

                foreach (var validator in Options.SecurityTokenValidators)
                {
                    if (validator.CanReadToken(openIdConnectMessage.IdToken))
                    {
                        principal = validator.ValidateToken(openIdConnectMessage.IdToken, validationParameters, out validatedToken);
                        jwt = validatedToken as JwtSecurityToken;
                        if (jwt == null)
                        {
                            throw new InvalidOperationException("Validated Security Token must be a JwtSecurityToken was: " + (validatedToken == null ? "null" : validatedToken.GetType().ToString()));
                        }
                    }
                }

                if (validatedToken == null)
                {
                    throw new InvalidOperationException("No SecurityTokenValidator found for token: " + openIdConnectMessage.IdToken);
                }

                ticket = new AuthenticationTicket(principal, properties, Options.AuthenticationScheme);
                if (!string.IsNullOrWhiteSpace(openIdConnectMessage.SessionState))
                {
                    ticket.Properties.Dictionary[OpenIdConnectSessionProperties.SessionState] = openIdConnectMessage.SessionState;
                }

                if (_configuration != null && !string.IsNullOrWhiteSpace(_configuration.CheckSessionIframe))
                {
                    ticket.Properties.Dictionary[OpenIdConnectSessionProperties.CheckSessionIFrame] = _configuration.CheckSessionIframe;
                }

                if (Options.UseTokenLifetime)
                {
                    // Override any session persistence to match the token lifetime.
                    DateTime issued = validatedToken.ValidFrom;
                    if (issued != DateTime.MinValue)
                    {
                        ticket.Properties.IssuedUtc = issued;
                    }

                    DateTime expires = validatedToken.ValidTo;
                    if (expires != DateTime.MinValue)
                    {
                        ticket.Properties.ExpiresUtc = expires;
                    }

                    ticket.Properties.AllowRefresh = false;
                }

                var securityTokenValidatedNotification = new SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    AuthenticationTicket = ticket,
                    ProtocolMessage = openIdConnectMessage
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

                var protocolValidationContext = new OpenIdConnectProtocolValidationContext
                {
                    AuthorizationCode = openIdConnectMessage.Code,
                    Nonce = RetrieveNonce(jwt.Payload.Nonce),
                };

                Options.ProtocolValidator.Validate(jwt, protocolValidationContext);
                if (openIdConnectMessage.Code != null)
                {
                    var authorizationCodeReceivedNotification = new AuthorizationCodeReceivedNotification(Context, Options)
                    {
                        AuthenticationTicket = ticket,
                        Code = openIdConnectMessage.Code,
                        JwtSecurityToken = jwt,
                        ProtocolMessage = openIdConnectMessage,
                        RedirectUri = ticket.Properties.Dictionary.ContainsKey(OpenIdConnectAuthenticationDefaults.RedirectUriUsedForCodeKey) ?
                                      ticket.Properties.Dictionary[OpenIdConnectAuthenticationDefaults.RedirectUriUsedForCodeKey] : string.Empty,
                    };

                    await Options.Notifications.AuthorizationCodeReceived(authorizationCodeReceivedNotification);
                    if (authorizationCodeReceivedNotification.HandledResponse)
                    {
                        return authorizationCodeReceivedNotification.AuthenticationTicket;
                    }

                    if (authorizationCodeReceivedNotification.Skipped)
                    {
                        return null;
                    }
                }

                return ticket;
            }
            catch (Exception exception)
            {
                _logger.WriteError("Exception occurred while processing message", exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    Options.ConfigurationManager.RequestRefresh();
                }

                var authenticationFailedNotification = new AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = openIdConnectMessage,
                    Exception = exception
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

        /// <summary>
        /// Adds the nonce to <see cref="HttpResponse.Cookies"/>.
        /// </summary>
        /// <param name="nonce">the nonce to remember.</param>
        /// <remarks><see cref="HttpResponse.Cookies.Append"/>is called to add a cookie with the name: 'OpenIdConnectAuthenticationDefaults.Nonce + <see cref="OpenIdConnectAuthenticationOptions.StringDataFormat.Protect"/>(nonce)'.
        /// The value of the cookie is: "N".</remarks>
        private void RememberNonce(string nonce)
        {
            if (string.IsNullOrWhiteSpace(nonce))
            {
                throw new ArgumentNullException("nonce");
            }

            Response.Cookies.Append(
                OpenIdConnectAuthenticationDefaults.CookieNoncePrefix + Options.StringDataFormat.Protect(nonce),
                NonceProperty,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps
                });
        }

        /// <summary>
        /// Searches <see cref="HttpRequest.Cookies"/> for a matching nonce.
        /// </summary>
        /// <param name="nonceExpectedValue">the nonce that was found in the jwt token.</param>
        /// <returns>'nonceExpectedValue' if a cookie is found that matches, null otherwise.</returns>
        /// <remarks>Examine <see cref="HttpRequest.Cookies.Keys"/> that start with the prefix: 'OpenIdConnectAuthenticationDefaults.Nonce'. 
        /// <see cref="OpenIdConnectAuthenticationOptions.StringDataFormat.Unprotect"/> is used to obtain the actual 'nonce'. If the nonce is found, then <see cref="HttpResponse.Cookies.Delete"/> is called.</remarks>
        private string RetrieveNonce(string nonceExpectedValue)
        {
            if (nonceExpectedValue == null)
            {
                return null;
            }

            foreach (var nonceKey in Request.Cookies.Keys)
            {
                if (nonceKey.StartsWith(OpenIdConnectAuthenticationDefaults.CookieNoncePrefix))
                {
                    try
                    {
                        string nonceDecodedValue = Options.StringDataFormat.Unprotect(nonceKey.Substring(OpenIdConnectAuthenticationDefaults.CookieNoncePrefix.Length, nonceKey.Length - OpenIdConnectAuthenticationDefaults.CookieNoncePrefix.Length));
                        if (nonceDecodedValue == nonceExpectedValue)
                        {
                            var cookieOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = Request.IsHttps
                            };

                            Response.Cookies.Delete(nonceKey, cookieOptions);
                            return nonceExpectedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteWarning("Failed to un-protect the nonce cookie.", ex);
                    }
                }
            }

            return null;
        }

        private AuthenticationProperties GetPropertiesFromState(string state)
        {
            // assume a well formed query string: <a=b&>OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey=kasjd;fljasldkjflksdj<&c=d>
            int startIndex = 0;
            if (string.IsNullOrWhiteSpace(state) || (startIndex = state.IndexOf(OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey, StringComparison.Ordinal)) == -1)
            {
                return null;
            }

            int authenticationIndex = startIndex + OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey.Length;
            if (authenticationIndex == -1 || authenticationIndex == state.Length || state[authenticationIndex] != '=')
            {
                return null;
            }

            // scan rest of string looking for '&'
            authenticationIndex++;
            int endIndex = state.Substring(authenticationIndex, state.Length - authenticationIndex).IndexOf("&", StringComparison.Ordinal);

            // -1 => no other parameters are after the AuthenticationPropertiesKey
            if (endIndex == -1)
            {
                return Options.StateDataFormat.Unprotect(Uri.UnescapeDataString(state.Substring(authenticationIndex).Replace('+', ' ')));
            }
            else
            {
                return Options.StateDataFormat.Unprotect(Uri.UnescapeDataString(state.Substring(authenticationIndex, endIndex).Replace('+', ' ')));
            }
        }

        /// <summary>
        /// Calls InvokeReplyPathAsync
        /// </summary>
        /// <returns>True if the request was handled, false if the next middleware should be invoked.</returns>
        public override Task<bool> InvokeAsync()
        {
            return InvokeReplyPathAsync();
        }

        private async Task<bool> InvokeReplyPathAsync()
        {
            AuthenticationTicket ticket = await AuthenticateAsync();

            if (ticket != null)
            {
                if (ticket.Principal != null)
                {
                    Request.HttpContext.Response.SignIn(ticket.AuthenticationScheme, ticket.Principal, ticket.Properties);
                }

                // Redirect back to the original secured resource, if any.
                if (!string.IsNullOrWhiteSpace(ticket.Properties.RedirectUri))
                {
                    Response.Redirect(ticket.Properties.RedirectUri);
                    return true;
                }
            }

            return false;
        }
    }
}
