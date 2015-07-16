// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// A per-request authentication handler for the OpenIdConnectAuthenticationMiddleware.
    /// </summary>
    public class OpenIdConnectAuthenticationHandler : AuthenticationHandler<OpenIdConnectAuthenticationOptions>
    {
        private const string NonceProperty = "N";
        private const string UriSchemeDelimiter = "://";
        private OpenIdConnectConfiguration _configuration;

        protected HttpClient Backchannel { get; private set; }

        public OpenIdConnectAuthenticationHandler(HttpClient backchannel)
        {
            Backchannel = backchannel;
        }

        /// <summary>
        /// Handles Signout
        /// </summary>
        /// <returns></returns>
        protected override async Task HandleSignOutAsync(SignOutContext signout)
        {
            if (signout != null)
            {
                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                var message = new OpenIdConnectMessage()
                {
                    IssuerAddress = _configuration == null ? string.Empty : (_configuration.EndSessionEndpoint ?? string.Empty),
                    RequestType = OpenIdConnectRequestType.LogoutRequest,
                };

                // Set End_Session_Endpoint in order:
                // 1. properties.Redirect
                // 2. Options.PostLogoutRedirectUri
                var properties = new AuthenticationProperties(signout.Properties);
                if (!string.IsNullOrEmpty(properties.RedirectUri))
                {
                    message.PostLogoutRedirectUri = properties.RedirectUri;
                }
                else if (!string.IsNullOrEmpty(Options.PostLogoutRedirectUri))
                {
                    message.PostLogoutRedirectUri = Options.PostLogoutRedirectUri;
                }

                if (Options.Notifications.RedirectToIdentityProvider != null)
                {
                    var redirectToIdentityProviderNotification = new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                    {
                        ProtocolMessage = message
                    };

                    await Options.Notifications.RedirectToIdentityProvider(redirectToIdentityProviderNotification);
                    if (redirectToIdentityProviderNotification.HandledResponse)
                    {
                        Logger.LogVerbose(Resources.OIDCH_0034_RedirectToIdentityProviderNotificationHandledResponse);
                        return;
                    }
                    else if (redirectToIdentityProviderNotification.Skipped)
                    {
                        Logger.LogVerbose(Resources.OIDCH_0035_RedirectToIdentityProviderNotificationSkipped);
                        return;
                    }

                    message = redirectToIdentityProviderNotification.ProtocolMessage;
                }

                var redirectUri = message.CreateLogoutRequestUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    Logger.LogWarning(Resources.OIDCH_0051_RedirectUriLogoutIsNotWellFormed, redirectUri);
                }

                Response.Redirect(redirectUri);
            }
        }

        /// <summary>
        /// Responds to a 401 Challenge. Sends an OpenIdConnect message to the 'identity authority' to obtain an identity.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Uses log id's OIDCH-0026 - OIDCH-0050, next num: 37</remarks>
        protected override async Task<bool> HandleUnauthorizedAsync([NotNull] ChallengeContext context)
        {
            Logger.LogDebug(Resources.OIDCH_0026_ApplyResponseChallengeAsync, this.GetType());

            // order for local RedirectUri
            // 1. challenge.Properties.RedirectUri
            // 2. CurrentUri if Options.DefaultToCurrentUriOnRedirect is true)
            AuthenticationProperties properties = new AuthenticationProperties(context.Properties);

            if (!string.IsNullOrEmpty(properties.RedirectUri))
            {
                Logger.LogDebug(Resources.OIDCH_0030_Using_Properties_RedirectUri, properties.RedirectUri);
            }
            else if (Options.DefaultToCurrentUriOnRedirect)
            {
                Logger.LogDebug(Resources.OIDCH_0032_UsingCurrentUriRedirectUri, CurrentUri);
                properties.RedirectUri = CurrentUri;
            }

            if (_configuration == null && Options.ConfigurationManager != null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            var message = new OpenIdConnectMessage
            {
                ClientId = Options.ClientId,
                IssuerAddress = _configuration?.AuthorizationEndpoint ?? string.Empty,
                RedirectUri = Options.RedirectUri,
                // [brentschmaltz] - #215 this should be a property on RedirectToIdentityProviderNotification not on the OIDCMessage.
                RequestType = OpenIdConnectRequestType.AuthenticationRequest,
                Resource = Options.Resource,
                ResponseType = Options.ResponseType,
                Scope = Options.Scope
            };

            // Omitting the response_mode parameter when it already corresponds to the default
            // response_mode used for the specified response_type is recommended by the specifications.
            // See http://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#ResponseModes
            if (!string.Equals(Options.ResponseType, OpenIdConnectResponseTypes.Code, StringComparison.Ordinal) ||
                !string.Equals(Options.ResponseMode, OpenIdConnectResponseModes.Query, StringComparison.Ordinal))
            {
                message.ResponseMode = Options.ResponseMode;
            }

            if (Options.ProtocolValidator.RequireNonce)
            {
                message.Nonce = Options.ProtocolValidator.GenerateNonce();
                if (Options.CacheNonces)
                {
                    if (await Options.NonceCache.GetAsync(message.Nonce) != null)
                    {
                        Logger.LogError(Resources.OIDCH_0033_NonceAlreadyExists, message.Nonce);
                        throw new OpenIdConnectProtocolException(string.Format(CultureInfo.InvariantCulture, Resources.OIDCH_0033_NonceAlreadyExists, message.Nonce));
                    }

                    await Options.NonceCache.SetAsync(message.Nonce, new byte[0], new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = Options.ProtocolValidator.NonceLifetime
                    });
                }
                else
                {
                    WriteNonceCookie(message.Nonce);
                }
            }

            if (Options.Notifications.RedirectToIdentityProvider != null)
            {
                var redirectToIdentityProviderNotification =
                    new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                    {
                        ProtocolMessage = message
                    };

                await Options.Notifications.RedirectToIdentityProvider(redirectToIdentityProviderNotification);
                if (redirectToIdentityProviderNotification.HandledResponse)
                {
                    Logger.LogVerbose(Resources.OIDCH_0034_RedirectToIdentityProviderNotificationHandledResponse);
                    return true;
                }
                else if (redirectToIdentityProviderNotification.Skipped)
                {
                    Logger.LogVerbose(Resources.OIDCH_0035_RedirectToIdentityProviderNotificationSkipped);
                    return false;
                }

                if (!string.IsNullOrEmpty(redirectToIdentityProviderNotification.ProtocolMessage.State))
                {
                    properties.Items[OpenIdConnectAuthenticationDefaults.UserstatePropertiesKey] = redirectToIdentityProviderNotification.ProtocolMessage.State;
                }

                message = redirectToIdentityProviderNotification.ProtocolMessage;
            }

            var redirectUriForCode = message.RedirectUri;
            if (string.IsNullOrEmpty(redirectUriForCode))
            {
                Logger.LogDebug(Resources.OIDCH_0031_Using_Options_RedirectUri, Options.RedirectUri);
                redirectUriForCode = Options.RedirectUri;
            }

            if (!string.IsNullOrEmpty(redirectUriForCode))
            {
                // When redeeming a 'code' for an AccessToken, this value is needed
                properties.Items.Add(OpenIdConnectAuthenticationDefaults.RedirectUriForCodePropertiesKey, redirectUriForCode);
            }

            message.State = Options.StateDataFormat.Protect(properties);

            var redirectUri = message.CreateAuthenticationRequestUrl();
            if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
            {
                Logger.LogWarning(Resources.OIDCH_0036_UriIsNotWellFormed, redirectUri);
            }

            Response.Redirect(redirectUri);
            return true;
        }

        /// <summary>
        /// Invoked to process incoming OpenIdConnect messages.
        /// </summary>
        /// <returns>An <see cref="AuthenticationTicket"/> if successful.</returns>
        /// <remarks>Uses log id's OIDCH-0000 - OIDCH-0025</remarks>
        protected override async Task<AuthenticationTicket> HandleAuthenticateAsync()
        {
            Logger.LogDebug(Resources.OIDCH_0000_AuthenticateCoreAsync, this.GetType());

            // Allow login to be constrained to a specific path. Need to make this runtime configurable.
            if (Options.CallbackPath.HasValue && Options.CallbackPath != (Request.PathBase + Request.Path))
            {
                return null;
            }

            OpenIdConnectMessage message = null;

            if (string.Equals(Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                message = new OpenIdConnectMessage(Request.Query);

                // response_mode=query (explicit or not) and a response_type containing id_token
                // or token are not considered as a safe combination and MUST be rejected.
                // See http://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#Security
                if (!string.IsNullOrWhiteSpace(message.IdToken) || !string.IsNullOrWhiteSpace(message.Token))
                {
                    Logger.LogError("An OpenID Connect response cannot contain an identity token " +
                                    "or an access token when using response_mode=query");
                    return null;
                }
            }
            // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
            else if (string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
              && !string.IsNullOrWhiteSpace(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();
                Request.Body.Seek(0, SeekOrigin.Begin);
                message = new OpenIdConnectMessage(form);
            }

            if (message == null)
            {
                return null;
            }

            try
            {
                var messageReceivedNotification = await RunMessageReceivedNotificationAsync(message);
                if (messageReceivedNotification.HandledResponse)
                {
                    return messageReceivedNotification.AuthenticationTicket;
                }
                else if (messageReceivedNotification.Skipped)
                {
                    return null;
                }

                var properties = new AuthenticationProperties();

                // if state is missing, just log it
                if (string.IsNullOrEmpty(message.State))
                {
                    Logger.LogWarning(Resources.OIDCH_0004_MessageStateIsNullOrEmpty);
                }
                else
                {
                    // if state exists and we failed to 'unprotect' this is not a message we should process.
                    properties = Options.StateDataFormat.Unprotect(Uri.UnescapeDataString(message.State));
                    if (properties == null)
                    {
                        Logger.LogError(Resources.OIDCH_0005_MessageStateIsInvalid);
                        return null;
                    }

                    string userstate = null;
                    properties.Items.TryGetValue(OpenIdConnectAuthenticationDefaults.UserstatePropertiesKey, out userstate);
                    message.State = userstate;
                }

                // if any of the error fields are set, throw error null
                if (!string.IsNullOrEmpty(message.Error))
                {
                    Logger.LogError(Resources.OIDCH_0006_MessageContainsError, message.Error, message.ErrorDescription ?? "ErrorDecription null", message.ErrorUri ?? "ErrorUri null");
                    throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.OIDCH_0006_MessageContainsError, message.Error, message.ErrorDescription ?? "ErrorDecription null", message.ErrorUri ?? "ErrorUri null"));
                }

                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    Logger.LogVerbose(Resources.OIDCH_0007_UpdatingConfiguration);
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                if (string.IsNullOrEmpty(message.IdToken) && !string.IsNullOrEmpty(message.Code))
                {
                    return await HandleCodeOnlyFlow(message, properties);
                }
                else if (!string.IsNullOrEmpty(message.IdToken))
                {
                    return await HandleIdTokenFlows(message, properties);
                }
                else
                {
                    Logger.LogDebug(Resources.OIDCH_0045_Id_Token_Code_Missing);
                    return null;
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(Resources.OIDCH_0017_ExceptionOccurredWhileProcessingMessage, exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the notification.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    if (Options.ConfigurationManager != null)
                    {
                        Logger.LogVerbose(Resources.OIDCH_0021_AutomaticConfigurationRefresh);
                        Options.ConfigurationManager.RequestRefresh();
                    }
                }

                var authenticationFailedNotification = await RunAuthenticationFailedNotificationAsync(message, exception);
                if (authenticationFailedNotification.HandledResponse)
                {
                    return authenticationFailedNotification.AuthenticationTicket;
                }
                else if (authenticationFailedNotification.Skipped)
                {
                    return null;
                }

                throw;
            }
        }

        private async Task<AuthenticationTicket> HandleCodeOnlyFlow(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            AuthenticationTicket ticket = null;
            JwtSecurityToken jwt = null;

            OpenIdConnectTokenEndpointResponse tokenEndpointResponse = null;
            string idToken = null;
            var authorizationCodeReceivedNotification = await RunAuthorizationCodeReceivedNotificationAsync(message, properties, ticket, jwt);
            if (authorizationCodeReceivedNotification.HandledResponse)
            {
                return authorizationCodeReceivedNotification.AuthenticationTicket;
            }
            else if (authorizationCodeReceivedNotification.Skipped)
            {
                return null;
            }

            // Redeeming authorization code for tokens
            Logger.LogDebug(Resources.OIDCH_0038_Redeeming_Auth_Code, message.Code);

            tokenEndpointResponse = await RedeemAuthorizationCodeAsync(message.Code, authorizationCodeReceivedNotification.RedirectUri);
            idToken = tokenEndpointResponse.Message.IdToken;

            var authorizationCodeRedeemedNotification = await RunAuthorizationCodeRedeemedNotificationAsync(message, tokenEndpointResponse);
            if (authorizationCodeRedeemedNotification.HandledResponse)
            {
                return authorizationCodeRedeemedNotification.AuthenticationTicket;
            }
            else if (authorizationCodeRedeemedNotification.Skipped)
            {
                return null;
            }

            // no need to validate signature when token is received using "code flow" as per spec [http://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation].
            var validationParameters = Options.TokenValidationParameters.Clone();
            validationParameters.ValidateSignature = false;

            ticket = ValidateToken(idToken, message, properties, validationParameters, out jwt);

            if (Options.GetClaimsFromUserInfoEndpoint)
            {
                Logger.LogDebug(Resources.OIDCH_0040_Sending_Request_UIEndpoint);
                ticket = await GetUserInformationAsync(properties, tokenEndpointResponse.Message, ticket);
            }

            var securityTokenValidatedNotification = await RunSecurityTokenValidatedNotificationAsync(message, ticket);
            if (securityTokenValidatedNotification.HandledResponse)
            {
                return securityTokenValidatedNotification.AuthenticationTicket;
            }
            else if (securityTokenValidatedNotification.Skipped)
            {
                return null;
            }

            // If id_token is received using code only flow, no need to validate chash.
            await ValidateOpenIdConnectProtocolAsync(jwt, message, false);

            return ticket;
        }

        private async Task<AuthenticationTicket> HandleIdTokenFlows(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            AuthenticationTicket ticket = null;
            JwtSecurityToken jwt = null;

            var securityTokenReceivedNotification = await RunSecurityTokenReceivedNotificationAsync(message);
            if (securityTokenReceivedNotification.HandledResponse)
            {
                return securityTokenReceivedNotification.AuthenticationTicket;
            }
            else if (securityTokenReceivedNotification.Skipped)
            {
                return null;
            }

            var validationParameters = Options.TokenValidationParameters.Clone();
            ticket = ValidateToken(message.IdToken, message, properties, validationParameters, out jwt);

            var securityTokenValidatedNotification = await RunSecurityTokenValidatedNotificationAsync(message, ticket);
            if (securityTokenValidatedNotification.HandledResponse)
            {
                return securityTokenValidatedNotification.AuthenticationTicket;
            }
            else if (securityTokenValidatedNotification.Skipped)
            {
                return null;
            }

            await ValidateOpenIdConnectProtocolAsync(jwt, message);

            if (message.Code != null)
            {
                var authorizationCodeReceivedNotification = await RunAuthorizationCodeReceivedNotificationAsync(message, properties, ticket, jwt);
                if (authorizationCodeReceivedNotification.HandledResponse)
                {
                    return authorizationCodeReceivedNotification.AuthenticationTicket;
                }
                else if (authorizationCodeReceivedNotification.Skipped)
                {
                    return null;
                }
            }

            return ticket;
        }

        /// <summary>
        /// Redeems the authorization code for tokens at the token endpoint
        /// </summary>
        /// <param name="authorizationCode">The authorization code to redeem.</param>
        /// <param name="redirectUri">Uri that was passed in the request sent for the authorization code.</param>
        /// <returns>OpenIdConnect message that has tokens inside it.</returns>
        protected virtual async Task<OpenIdConnectTokenEndpointResponse> RedeemAuthorizationCodeAsync(string authorizationCode, string redirectUri)
        {
            var openIdMessage = new OpenIdConnectMessage()
            {
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                Code = authorizationCode,
                GrantType = "authorization_code",
                RedirectUri = redirectUri
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration.TokenEndpoint);
            requestMessage.Content = new FormUrlEncodedContent(openIdMessage.Parameters);
            var responseMessage = await Backchannel.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();
            var tokenResonse = await responseMessage.Content.ReadAsStringAsync();
            var jsonTokenResponse = JObject.Parse(tokenResonse);
            return new OpenIdConnectTokenEndpointResponse(jsonTokenResponse);
        }

        /// <summary>
        /// Goes to UserInfo endpoint to retrieve additional claims and add any unique claims to the given identity.
        /// </summary>
        /// <param name="properties">Authentication Properties</param>
        /// <param name="message">message that is being processed</param>
        /// <param name="ticket">authentication ticket with claims principal and identities</param>
        /// <returns>Authentication ticket with identity with additional claims, if any.</returns>
        protected virtual async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, OpenIdConnectMessage message, AuthenticationTicket ticket)
        {
            string userInfoEndpoint = null;
            if (_configuration != null)
            {
                userInfoEndpoint = _configuration.UserInfoEndpoint;
            }

            if (string.IsNullOrEmpty(userInfoEndpoint))
            {
                Logger.LogWarning(Resources.OIDCH_0046_UserInfo_Endpoint_Not_Set);
                return ticket;
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", message.AccessToken);
            var responseMessage = await Backchannel.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();
            var userInfoResponse = await responseMessage.Content.ReadAsStringAsync();
            var user = JObject.Parse(userInfoResponse);

            var identity = (ClaimsIdentity)ticket.Principal.Identity;
            var subjectClaimType = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (subjectClaimType == null)
            {
                Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.OIDCH_0041_Subject_Claim_Not_Found, identity.ToString()));
                return ticket;
            }

            var userInfoSubjectClaimValue = user.Value<string>(JwtRegisteredClaimNames.Sub);

            // check if the sub claim matches
            if (userInfoSubjectClaimValue == null || !string.Equals(userInfoSubjectClaimValue, subjectClaimType.Value, StringComparison.Ordinal))
            {
                Logger.LogError(Resources.OIDCH_0039_Subject_Claim_Mismatch);
                return ticket;
            }

            foreach (var claim in identity.Claims)
            {
                // If this claimType is mapped by the JwtSeurityTokenHandler, then this property will be set
                var shortClaimTypeName = claim.Properties.ContainsKey(JwtSecurityTokenHandler.ShortClaimTypeProperty) ?
                    claim.Properties[JwtSecurityTokenHandler.ShortClaimTypeProperty] : string.Empty;

                // checking if claim in the identity (generated from id_token) has the same type as a claim retrieved from userinfo endpoint
                JToken value;
                var isClaimIncluded = user.TryGetValue(claim.Type, out value) || user.TryGetValue(shortClaimTypeName, out value);

                // if a same claim exists (matching both type and value) both in id_token identity and userinfo response, remove the json entry from the userinfo response
                if (isClaimIncluded && claim.Value.Equals(value.ToString(), StringComparison.Ordinal))
                {
                    if (!user.Remove(claim.Type))
                    {
                        user.Remove(shortClaimTypeName);
                    }
                }
            }

            // adding remaining unique claims from userinfo endpoint to the identity
            foreach (var pair in user)
            {
                JToken value;
                var claimValue = user.TryGetValue(pair.Key, out value) ? value.ToString() : null;
                identity.AddClaim(new Claim(pair.Key, claimValue, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            return new AuthenticationTicket(new ClaimsPrincipal(identity), ticket.Properties, ticket.AuthenticationScheme);
        }

        /// <summary>
        /// Adds the nonce to <see cref="HttpResponse.Cookies"/>.
        /// </summary>
        /// <param name="nonce">the nonce to remember.</param>
        /// <remarks><see cref="HttpResponse.Cookies.Append"/>is called to add a cookie with the name: 'OpenIdConnectAuthenticationDefaults.Nonce + <see cref="OpenIdConnectAuthenticationOptions.StringDataFormat.Protect"/>(nonce)'.
        /// The value of the cookie is: "N".</remarks>
        private void WriteNonceCookie(string nonce)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                throw new ArgumentNullException(nameof(nonce));
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
        /// <param name="nonce">the nonce that we are looking for.</param>
        /// <returns>echos 'nonce' if a cookie is found that matches, null otherwise.</returns>
        /// <remarks>Examine <see cref="HttpRequest.Cookies.Keys"/> that start with the prefix: 'OpenIdConnectAuthenticationDefaults.Nonce'. 
        /// <see cref="OpenIdConnectAuthenticationOptions.StringDataFormat.Unprotect"/> is used to obtain the actual 'nonce'. If the nonce is found, then <see cref="HttpResponse.Cookies.Delete"/> is called.</remarks>
        private string ReadNonceCookie(string nonce)
        {
            if (nonce == null)
            {
                return null;
            }

            foreach (var nonceKey in Request.Cookies.Keys)
            {
                if (nonceKey.StartsWith(OpenIdConnectAuthenticationDefaults.CookieNoncePrefix))
                {
                    try
                    {
                        var nonceDecodedValue = Options.StringDataFormat.Unprotect(nonceKey.Substring(OpenIdConnectAuthenticationDefaults.CookieNoncePrefix.Length, nonceKey.Length - OpenIdConnectAuthenticationDefaults.CookieNoncePrefix.Length));
                        if (nonceDecodedValue == nonce)
                        {
                            var cookieOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = Request.IsHttps
                            };

                            Response.Cookies.Delete(nonceKey, cookieOptions);
                            return nonce;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Failed to un-protect the nonce cookie.", ex);
                    }
                }
            }

            return null;
        }

        private AuthenticationProperties GetPropertiesFromState(string state)
        {
            // assume a well formed query string: <a=b&>OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey=kasjd;fljasldkjflksdj<&c=d>
            var startIndex = 0;
            if (string.IsNullOrWhiteSpace(state) || (startIndex = state.IndexOf(OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey, StringComparison.Ordinal)) == -1)
            {
                return null;
            }

            var authenticationIndex = startIndex + OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey.Length;
            if (authenticationIndex == -1 || authenticationIndex == state.Length || state[authenticationIndex] != '=')
            {
                return null;
            }

            // scan rest of string looking for '&'
            authenticationIndex++;
            var endIndex = state.Substring(authenticationIndex, state.Length - authenticationIndex).IndexOf("&", StringComparison.Ordinal);

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

        private async Task<MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>> RunMessageReceivedNotificationAsync(OpenIdConnectMessage message)
        {
            Logger.LogDebug(Resources.OIDCH_0001_MessageReceived, message.BuildRedirectUrl());
            var messageReceivedNotification =
                new MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = message
                };

            await Options.Notifications.MessageReceived(messageReceivedNotification);
            if (messageReceivedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0002_MessageReceivedNotificationHandledResponse);
            }
            else if (messageReceivedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0003_MessageReceivedNotificationSkipped);
            }

            return messageReceivedNotification;
        }

        private async Task<AuthorizationCodeReceivedNotification> RunAuthorizationCodeReceivedNotificationAsync(OpenIdConnectMessage message, AuthenticationProperties properties, AuthenticationTicket ticket, JwtSecurityToken jwt)
        {
            var redirectUri = properties.Items.ContainsKey(OpenIdConnectAuthenticationDefaults.RedirectUriForCodePropertiesKey) ?
                properties.Items[OpenIdConnectAuthenticationDefaults.RedirectUriForCodePropertiesKey] : Options.RedirectUri;

            Logger.LogDebug(Resources.OIDCH_0014_AuthorizationCodeReceived, message.Code);

            var authorizationCodeReceivedNotification = new AuthorizationCodeReceivedNotification(Context, Options)
            {
                Code = message.Code,
                ProtocolMessage = message,
                RedirectUri = redirectUri,
                AuthenticationTicket = ticket,
                JwtSecurityToken = jwt
            };

            await Options.Notifications.AuthorizationCodeReceived(authorizationCodeReceivedNotification);
            if (authorizationCodeReceivedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0015_AuthorizationCodeReceivedNotificationHandledResponse);
            }
            else if (authorizationCodeReceivedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0016_AuthorizationCodeReceivedNotificationSkipped);
            }

            return authorizationCodeReceivedNotification;
        }

        private async Task<AuthorizationCodeRedeemedNotification> RunAuthorizationCodeRedeemedNotificationAsync(OpenIdConnectMessage message, OpenIdConnectTokenEndpointResponse tokenEndpointResponse)
        {
            Logger.LogDebug(Resources.OIDCH_0042_AuthorizationCodeRedeemed, message.Code);
            var authorizationCodeRedeemedNotification = new AuthorizationCodeRedeemedNotification(Context, Options)
            {
                Code = message.Code,
                ProtocolMessage = message,
                TokenEndpointResponse = tokenEndpointResponse
            };

            await Options.Notifications.AuthorizationCodeRedeemed(authorizationCodeRedeemedNotification);
            if (authorizationCodeRedeemedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0043_AuthorizationCodeRedeemedNotificationHandledResponse);
            }
            else if (authorizationCodeRedeemedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0044_AuthorizationCodeRedeemedNotificationSkipped);
            }
            return authorizationCodeRedeemedNotification;
        }

        private async Task<SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>> RunSecurityTokenReceivedNotificationAsync(OpenIdConnectMessage message)
        {
            Logger.LogDebug(Resources.OIDCH_0020_IdTokenReceived, message.IdToken);
            var securityTokenReceivedNotification =
                new SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = message,
                };

            await Options.Notifications.SecurityTokenReceived(securityTokenReceivedNotification);
            if (securityTokenReceivedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0008_SecurityTokenReceivedNotificationHandledResponse);
            }
            else if (securityTokenReceivedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0009_SecurityTokenReceivedNotificationSkipped);
            }

            return securityTokenReceivedNotification;
        }

        private async Task<SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>> RunSecurityTokenValidatedNotificationAsync(OpenIdConnectMessage message, AuthenticationTicket ticket)
        {
            var securityTokenValidatedNotification =
                new SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    AuthenticationTicket = ticket,
                    ProtocolMessage = message
                };

            await Options.Notifications.SecurityTokenValidated(securityTokenValidatedNotification);
            if (securityTokenValidatedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0012_SecurityTokenValidatedNotificationHandledResponse);
            }
            else if (securityTokenValidatedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0013_SecurityTokenValidatedNotificationSkipped);
            }

            return securityTokenValidatedNotification;
        }

        private async Task<AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>> RunAuthenticationFailedNotificationAsync(OpenIdConnectMessage message, Exception exception)
        {
            var authenticationFailedNotification =
                new AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
                {
                    ProtocolMessage = message,
                    Exception = exception
                };

            await Options.Notifications.AuthenticationFailed(authenticationFailedNotification);
            if (authenticationFailedNotification.HandledResponse)
            {
                Logger.LogVerbose(Resources.OIDCH_0018_AuthenticationFailedNotificationHandledResponse);
            }
            else if (authenticationFailedNotification.Skipped)
            {
                Logger.LogVerbose(Resources.OIDCH_0019_AuthenticationFailedNotificationSkipped);
            }

            return authenticationFailedNotification;
        }

        private AuthenticationTicket ValidateToken(string idToken, OpenIdConnectMessage message, AuthenticationProperties properties, TokenValidationParameters validationParameters, out JwtSecurityToken jwt)
        {
            AuthenticationTicket ticket = null;
            jwt = null;

            if (_configuration != null)
            {
                if (string.IsNullOrEmpty(validationParameters.ValidIssuer))
                {
                    validationParameters.ValidIssuer = _configuration.Issuer;
                }
                else if (!string.IsNullOrEmpty(_configuration.Issuer))
                {
                    validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(new[] { _configuration.Issuer }) ?? new[] { _configuration.Issuer };
                }

                validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(_configuration.SigningKeys) ?? _configuration.SigningKeys;
            }

            SecurityToken validatedToken = null;
            ClaimsPrincipal principal = null;
            foreach (var validator in Options.SecurityTokenValidators)
            {
                if (validator.CanReadToken(idToken))
                {
                    principal = validator.ValidateToken(idToken, validationParameters, out validatedToken);
                    jwt = validatedToken as JwtSecurityToken;
                    if (jwt == null)
                    {
                        Logger.LogError(Resources.OIDCH_0010_ValidatedSecurityTokenNotJwt, validatedToken?.GetType());
                        throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.OIDCH_0010_ValidatedSecurityTokenNotJwt, validatedToken?.GetType()));
                    }
                }
            }

            if (validatedToken == null)
            {
                Logger.LogError(Resources.OIDCH_0011_UnableToValidateToken, idToken);
                throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.OIDCH_0011_UnableToValidateToken, idToken));
            }

            ticket = new AuthenticationTicket(principal, properties, Options.AuthenticationScheme);
            if (!string.IsNullOrEmpty(message.SessionState))
            {
                ticket.Properties.Items[OpenIdConnectSessionProperties.SessionState] = message.SessionState;
            }

            if (_configuration != null && !string.IsNullOrEmpty(_configuration.CheckSessionIframe))
            {
                ticket.Properties.Items[OpenIdConnectSessionProperties.CheckSessionIFrame] = _configuration.CheckSessionIframe;
            }

            // Rename?
            if (Options.UseTokenLifetime)
            {
                var issued = validatedToken.ValidFrom;
                if (issued != DateTime.MinValue)
                {
                    ticket.Properties.IssuedUtc = issued;
                }

                var expires = validatedToken.ValidTo;
                if (expires != DateTime.MinValue)
                {
                    ticket.Properties.ExpiresUtc = expires;
                }
            }

            return ticket;
        }

        private async Task ValidateOpenIdConnectProtocolAsync(JwtSecurityToken jwt, OpenIdConnectMessage message, bool ValidateCHash = true)
        {
            string nonce = jwt.Payload.Nonce;
            if (Options.CacheNonces)
            {
                if (await Options.NonceCache.GetAsync(nonce) != null)
                {
                    await Options.NonceCache.RemoveAsync(nonce);
                }
                else
                {
                    // If the nonce cannot be removed, it was
                    // already used and MUST be rejected.
                    nonce = null;
                }
            }
            else
            {
                nonce = ReadNonceCookie(nonce);
            }

            var protocolValidationContext = new OpenIdConnectProtocolValidationContext
            {
                Nonce = nonce
            };

            // If authorization code is null, protocol validator does not validate the chash
            if (ValidateCHash)
            {
                protocolValidationContext.AuthorizationCode = message.Code;
            }

            Options.ProtocolValidator.Validate(jwt, protocolValidationContext);
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
            var ticket = await HandleAuthenticateOnceAsync();
            if (ticket != null)
            {
                if (ticket.Principal != null)
                {
                    await Request.HttpContext.Authentication.SignInAsync(Options.SignInScheme, ticket.Principal, ticket.Properties);
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
