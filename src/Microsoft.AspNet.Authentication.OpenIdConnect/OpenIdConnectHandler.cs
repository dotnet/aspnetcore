// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// A per-request authentication handler for the OpenIdConnectAuthenticationMiddleware.
    /// </summary>
    public class OpenIdConnectHandler : RemoteAuthenticationHandler<OpenIdConnectOptions>
    {
        private const string NonceProperty = "N";
        private const string UriSchemeDelimiter = "://";

        private const string InputTagFormat = @"<input type=""hidden"" name=""{0}"" value=""{1}"" />";
        private const string HtmlFormFormat = @"<!doctype html>
<html>
<head>
    <title>Please wait while you're being redirected to the identity provider</title>
</head>
<body>
    <form name=""form"" method=""post"" action=""{0}"">
        {1}
        <noscript>Click here to finish the process: <input type=""submit"" /></noscript>
    </form>
    <script>document.form.submit();</script>
</body>
</html>";

        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

        private OpenIdConnectConfiguration _configuration;

        protected HttpClient Backchannel { get; private set; }

        protected IHtmlEncoder HtmlEncoder { get; private set; }

        public OpenIdConnectHandler(HttpClient backchannel, IHtmlEncoder htmlEncoder)
        {
            Backchannel = backchannel;
            HtmlEncoder = htmlEncoder;
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

                if (!string.IsNullOrEmpty(Options.SignInScheme))
                {
                    var principal = await Context.Authentication.AuthenticateAsync(Options.SignInScheme);

                    message.IdTokenHint = principal?.FindFirst(OpenIdConnectParameterNames.IdToken)?.Value;
                }

                var redirectContext = new RedirectContext(Context, Options)
                {
                    ProtocolMessage = message
                };

                await Options.Events.RedirectToEndSessionEndpoint(redirectContext);
                if (redirectContext.HandledResponse)
                {
                    Logger.LogVerbose(1, "RedirectToEndSessionEndpoint.HandledResponse");
                    return;
                }
                else if (redirectContext.Skipped)
                {
                    Logger.LogVerbose(2, "RedirectToEndSessionEndpoint.Skipped");
                    return;
                }

                message = redirectContext.ProtocolMessage;

                if (Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.RedirectGet)
                {
                    var redirectUri = message.CreateLogoutRequestUrl();
                    if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                    {
                        Logger.LogWarning(3, "The query string for Logout is not a well-formed URI. Redirect URI: '{0}'.", redirectUri);
                    }

                    Response.Redirect(redirectUri);
                }
                else if (Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.FormPost)
                {
                    var inputs = new StringBuilder();
                    foreach (var parameter in message.Parameters)
                    {
                        var name = HtmlEncoder.HtmlEncode(parameter.Key);
                        var value = HtmlEncoder.HtmlEncode(parameter.Value);

                        var input = string.Format(CultureInfo.InvariantCulture, InputTagFormat, name, value);
                        inputs.AppendLine(input);
                    }

                    var issuer = HtmlEncoder.HtmlEncode(message.IssuerAddress);

                    var content = string.Format(CultureInfo.InvariantCulture, HtmlFormFormat, issuer, inputs);
                    var buffer = Encoding.UTF8.GetBytes(content);

                    Response.ContentLength = buffer.Length;
                    Response.ContentType = "text/html;charset=UTF-8";

                    // Emit Cache-Control=no-cache to prevent client caching.
                    Response.Headers[HeaderNames.CacheControl] = "no-cache";
                    Response.Headers[HeaderNames.Pragma] = "no-cache";
                    Response.Headers[HeaderNames.Expires] = "-1";

                    await Response.Body.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Responds to a 401 Challenge. Sends an OpenIdConnect message to the 'identity authority' to obtain an identity.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Uses log id's OIDCH-0026 - OIDCH-0050, next num: 37</remarks>
        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Logger.LogDebug(4, "Entering {0}." + nameof(HandleUnauthorizedAsync), GetType());

            // order for local RedirectUri
            // 1. challenge.Properties.RedirectUri
            // 2. CurrentUri if RedirectUri is not set)
            var properties = new AuthenticationProperties(context.Properties);

            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }
            Logger.LogDebug(5, "Using properties.RedirectUri for 'local redirect' post authentication: '{0}'.", properties.RedirectUri);

            if (_configuration == null && Options.ConfigurationManager != null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            var message = new OpenIdConnectMessage
            {
                ClientId = Options.ClientId,
                IssuerAddress = _configuration?.AuthorizationEndpoint ?? string.Empty,
                RedirectUri = Options.RedirectUri,
                Resource = Options.Resource,
                ResponseType = Options.ResponseType,
                Scope = string.Join(" ", Options.Scope)
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
                WriteNonceCookie(message.Nonce);
            }

            GenerateCorrelationId(properties);

            var redirectContext = new RedirectContext(Context, Options)
            {
                ProtocolMessage = message
            };

            await Options.Events.RedirectToAuthenticationEndpoint(redirectContext);
            if (redirectContext.HandledResponse)
            {
                Logger.LogVerbose(6, "RedirectToAuthenticationEndpoint.HandledResponse");
                return true;
            }
            else if (redirectContext.Skipped)
            {
                Logger.LogVerbose(7, "RedirectToAuthenticationEndpoint.Skipped");
                return false;
            }

            message = redirectContext.ProtocolMessage;

            if (!string.IsNullOrEmpty(message.State))
            {
                properties.Items[OpenIdConnectDefaults.UserstatePropertiesKey] = message.State;
            }

            var redirectUriForCode = message.RedirectUri;
            if (string.IsNullOrEmpty(redirectUriForCode))
            {
                Logger.LogDebug(8, "Using Options.RedirectUri for 'redirect_uri': '{0}'.", Options.RedirectUri);
                redirectUriForCode = Options.RedirectUri;
            }

            if (!string.IsNullOrEmpty(redirectUriForCode))
            {
                // When redeeming a 'code' for an AccessToken, this value is needed
                properties.Items.Add(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, redirectUriForCode);
            }

            message.State = Options.StateDataFormat.Protect(properties);

            if (Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.RedirectGet)
            {
                var redirectUri = message.CreateAuthenticationRequestUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    Logger.LogWarning(9, "The redirect URI is not well-formed. The URI is: '{0}'.", redirectUri);
                }

                Response.Redirect(redirectUri);
                return true;
            }
            else if (Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.FormPost)
            {
                var inputs = new StringBuilder();
                foreach (var parameter in message.Parameters)
                {
                    var name = HtmlEncoder.HtmlEncode(parameter.Key);
                    var value = HtmlEncoder.HtmlEncode(parameter.Value);

                    var input = string.Format(CultureInfo.InvariantCulture, InputTagFormat, name, value);
                    inputs.AppendLine(input);
                }

                var issuer = HtmlEncoder.HtmlEncode(message.IssuerAddress);

                var content = string.Format(CultureInfo.InvariantCulture, HtmlFormFormat, issuer, inputs);
                var buffer = Encoding.UTF8.GetBytes(content);

                Response.ContentLength = buffer.Length;
                Response.ContentType = "text/html;charset=UTF-8";

                // Emit Cache-Control=no-cache to prevent client caching.
                Response.Headers[HeaderNames.CacheControl] = "no-cache";
                Response.Headers[HeaderNames.Pragma] = "no-cache";
                Response.Headers[HeaderNames.Expires] = "-1";

                await Response.Body.WriteAsync(buffer, 0, buffer.Length);
                return true;
            }

            throw new NotImplementedException($"An unsupported authentication method has been configured: {Options.AuthenticationMethod}");
        }

        /// <summary>
        /// Invoked to process incoming OpenIdConnect messages.
        /// </summary>
        /// <returns>An <see cref="AuthenticationTicket"/> if successful.</returns>
        /// <remarks>Uses log id's OIDCH-0000 - OIDCH-0025</remarks>
        protected override async Task<AuthenticateResult> HandleRemoteAuthenticateAsync()
        {
            Logger.LogDebug(10, "Entering: {0}." + nameof(HandleRemoteAuthenticateAsync), GetType());

            OpenIdConnectMessage message = null;

            if (string.Equals(Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                message = new OpenIdConnectMessage(Request.Query.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));

                // response_mode=query (explicit or not) and a response_type containing id_token
                // or token are not considered as a safe combination and MUST be rejected.
                // See http://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#Security
                if (!string.IsNullOrEmpty(message.IdToken) || !string.IsNullOrEmpty(message.AccessToken))
                {
                    return AuthenticateResult.Failed("An OpenID Connect response cannot contain an " +
                            "identity token or an access token when using response_mode=query");
                }
            }
            // assumption: if the ContentType is "application/x-www-form-urlencoded" it should be safe to read as it is small.
            else if (string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
              && !string.IsNullOrEmpty(Request.ContentType)
              // May have media/type; charset=utf-8, allow partial match.
              && Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
              && Request.Body.CanRead)
            {
                var form = await Request.ReadFormAsync();
                message = new OpenIdConnectMessage(form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));
            }

            if (message == null)
            {
                return AuthenticateResult.Failed("No message.");
            }

            try
            {
                var messageReceivedContext = await RunMessageReceivedEventAsync(message);
                if (messageReceivedContext.HandledResponse)
                {
                    return AuthenticateResult.Success(messageReceivedContext.AuthenticationTicket);
                }
                else if (messageReceivedContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }
                message = messageReceivedContext.ProtocolMessage;

                // Fail if state is missing, it's required for the correlation id.
                if (string.IsNullOrEmpty(message.State))
                {
                    // This wasn't a valid ODIC message, it may not have been intended for us.
                    Logger.LogVerbose(11, "message.State is null or empty.");
                    return AuthenticateResult.Failed(Resources.MessageStateIsNullOrEmpty);
                }

                // if state exists and we failed to 'unprotect' this is not a message we should process.
                var properties = Options.StateDataFormat.Unprotect(Uri.UnescapeDataString(message.State));
                if (properties == null)
                {
                    Logger.LogError(12, "Unable to unprotect the message.State.");
                    return AuthenticateResult.Failed(Resources.MessageStateIsInvalid);
                }

                // if any of the error fields are set, throw error null
                if (!string.IsNullOrEmpty(message.Error))
                {
                    // REVIEW: this error formatting is pretty nuts
                    Logger.LogError(13, "Message contains error: '{0}', error_description: '{1}', error_uri: '{2}'.", message.Error, message.ErrorDescription ?? "ErrorDecription null", message.ErrorUri ?? "ErrorUri null");
                    return AuthenticateResult.Failed(new OpenIdConnectProtocolException(string.Format(CultureInfo.InvariantCulture, Resources.MessageContainsError, message.Error, message.ErrorDescription ?? "ErrorDecription null", message.ErrorUri ?? "ErrorUri null")));
                }

                string userstate = null;
                properties.Items.TryGetValue(OpenIdConnectDefaults.UserstatePropertiesKey, out userstate);
                message.State = userstate;

                if (!ValidateCorrelationId(properties))
                {
                    return AuthenticateResult.Failed("Correlation failed.");
                }

                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    Logger.LogVerbose(14, "Updating configuration");
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                Logger.LogDebug(15, "Authorization response received.");
                var authorizationResponseReceivedContext = new AuthorizationResponseReceivedContext(Context, Options)
                {
                    ProtocolMessage = message,
                    Properties = properties
                };
                await Options.Events.AuthorizationResponseReceived(authorizationResponseReceivedContext);
                if (authorizationResponseReceivedContext.HandledResponse)
                {
                    Logger.LogVerbose(16, "AuthorizationResponseReceived.HandledResponse");
                    return AuthenticateResult.Success(authorizationResponseReceivedContext.AuthenticationTicket);
                }
                else if (authorizationResponseReceivedContext.Skipped)
                {
                    Logger.LogVerbose(17, "AuthorizationResponseReceived.Skipped");
                    return AuthenticateResult.Success(ticket: null);
                }
                message = authorizationResponseReceivedContext.ProtocolMessage;
                properties = authorizationResponseReceivedContext.Properties;

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
                    Logger.LogDebug(18, "Cannot process the message. Both id_token and code are missing.");
                    return AuthenticateResult.Failed(Resources.IdTokenCodeMissing);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(19, "Exception occurred while processing message.", exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    if (Options.ConfigurationManager != null)
                    {
                        Logger.LogVerbose(20, "exception of type 'SecurityTokenSignatureKeyNotFoundException' thrown, Options.ConfigurationManager.RequestRefresh() called.");
                        Options.ConfigurationManager.RequestRefresh();
                    }
                }

                var authenticationFailedContext = await RunAuthenticationFailedEventAsync(message, exception);
                if (authenticationFailedContext.HandledResponse)
                {
                    return AuthenticateResult.Success(authenticationFailedContext.AuthenticationTicket);
                }
                else if (authenticationFailedContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }

                throw;
            }
        }

        // Authorization Code Flow
        private async Task<AuthenticateResult> HandleCodeOnlyFlow(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            AuthenticationTicket ticket = null;
            JwtSecurityToken jwt = null;

            Options.ProtocolValidator.ValidateAuthenticationResponse(new OpenIdConnectProtocolValidationContext()
            {
                ClientId = Options.ClientId,
                ProtocolMessage = message,
            });

            var authorizationCodeReceivedContext = await RunAuthorizationCodeReceivedEventAsync(message, properties, ticket, jwt);
            if (authorizationCodeReceivedContext.HandledResponse)
            {
                return AuthenticateResult.Success(authorizationCodeReceivedContext.AuthenticationTicket);
            }
            else if (authorizationCodeReceivedContext.Skipped)
            {
                return AuthenticateResult.Success(ticket: null);
            }
            message = authorizationCodeReceivedContext.ProtocolMessage;
            var code = authorizationCodeReceivedContext.Code;

            // Redeeming authorization code for tokens
            Logger.LogDebug(21, "Id Token is null. Redeeming code '{0}' for tokens.", code);

            var tokenEndpointResponse = await RedeemAuthorizationCodeAsync(code, authorizationCodeReceivedContext.RedirectUri);

            var authorizationCodeRedeemedContext = await RunTokenResponseReceivedEventAsync(message, tokenEndpointResponse);
            if (authorizationCodeRedeemedContext.HandledResponse)
            {
                return AuthenticateResult.Success(authorizationCodeRedeemedContext.AuthenticationTicket);
            }
            else if (authorizationCodeRedeemedContext.Skipped)
            {
                return AuthenticateResult.Success(ticket: null);
            }

            message = authorizationCodeRedeemedContext.ProtocolMessage;
            tokenEndpointResponse = authorizationCodeRedeemedContext.TokenEndpointResponse;

            // no need to validate signature when token is received using "code flow" as per spec [http://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation].
            var validationParameters = Options.TokenValidationParameters.Clone();
            validationParameters.ValidateSignature = false;

            ticket = ValidateToken(tokenEndpointResponse.IdToken, message, properties, validationParameters, out jwt);

            var nonce = jwt?.Payload.Nonce;
            if (!string.IsNullOrEmpty(nonce))
            {
                nonce = ReadNonceCookie(nonce);
            }

            Options.ProtocolValidator.ValidateTokenResponse(new OpenIdConnectProtocolValidationContext()
            {
                ClientId = Options.ClientId,
                ProtocolMessage = tokenEndpointResponse,
                ValidatedIdToken = jwt,
                Nonce = nonce
            });

            var authenticationValidatedContext = await RunAuthenticationValidatedEventAsync(message, ticket, tokenEndpointResponse);
            if (authenticationValidatedContext.HandledResponse)
            {
                return AuthenticateResult.Success(authenticationValidatedContext.AuthenticationTicket);
            }
            else if (authenticationValidatedContext.Skipped)
            {
                return AuthenticateResult.Success(ticket: null);
            }
            ticket = authenticationValidatedContext.AuthenticationTicket;

            if (Options.SaveTokensAsClaims)
            {
                // Persist the tokens extracted from the token response.
                SaveTokens(ticket.Principal, tokenEndpointResponse, saveRefreshToken: true);
            }

            if (Options.GetClaimsFromUserInfoEndpoint)
            {
                Logger.LogDebug(22, "Sending request to user info endpoint for retrieving claims.");
                ticket = await GetUserInformationAsync(tokenEndpointResponse, jwt, ticket);
            }

            return AuthenticateResult.Success(ticket);
        }

        // Implicit Flow or Hybrid Flow
        private async Task<AuthenticateResult> HandleIdTokenFlows(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            Logger.LogDebug(23, "'id_token' received: '{0}'", message.IdToken);

            JwtSecurityToken jwt = null;
            var validationParameters = Options.TokenValidationParameters.Clone();
            var ticket = ValidateToken(message.IdToken, message, properties, validationParameters, out jwt);

            var nonce = jwt?.Payload.Nonce;
            if (!string.IsNullOrEmpty(nonce))
            {
                nonce = ReadNonceCookie(nonce);
            }

            Options.ProtocolValidator.ValidateAuthenticationResponse(new OpenIdConnectProtocolValidationContext()
            {
                ClientId = Options.ClientId,
                ProtocolMessage = message,
                ValidatedIdToken = jwt,
                Nonce = nonce
            });

            var authenticationValidatedContext = await RunAuthenticationValidatedEventAsync(message, ticket, tokenEndpointResponse: null);
            if (authenticationValidatedContext.HandledResponse)
            {
                return AuthenticateResult.Success(authenticationValidatedContext.AuthenticationTicket);
            }
            else if (authenticationValidatedContext.Skipped)
            {
                return AuthenticateResult.Success(ticket: null);
            }
            message = authenticationValidatedContext.ProtocolMessage;
            ticket = authenticationValidatedContext.AuthenticationTicket;

            // Hybrid Flow
            if (message.Code != null)
            {
                var authorizationCodeReceivedContext = await RunAuthorizationCodeReceivedEventAsync(message, properties, ticket, jwt);
                if (authorizationCodeReceivedContext.HandledResponse)
                {
                    return AuthenticateResult.Success(authorizationCodeReceivedContext.AuthenticationTicket);
                }
                else if (authorizationCodeReceivedContext.Skipped)
                {
                    return AuthenticateResult.Success(ticket: null);
                }
                message = authorizationCodeReceivedContext.ProtocolMessage;
                ticket = authorizationCodeReceivedContext.AuthenticationTicket;

                if (Options.SaveTokensAsClaims)
                {
                    // TODO: call SaveTokens with the token response and set
                    // saveRefreshToken to true when the hybrid flow is fully implemented.
                    SaveTokens(ticket.Principal, message, saveRefreshToken: false);
                }
            }
            // Implicit Flow
            else
            {
                if (Options.SaveTokensAsClaims)
                {
                    // Note: don't save the refresh token when it is extracted from the authorization
                    // response, since it's not a valid parameter when using the implicit flow.
                    // See http://openid.net/specs/openid-connect-core-1_0.html#Authentication
                    // and https://tools.ietf.org/html/rfc6749#section-4.2.2.
                    SaveTokens(ticket.Principal, message, saveRefreshToken: false);
                }
            }

            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Redeems the authorization code for tokens at the token endpoint
        /// </summary>
        /// <param name="authorizationCode">The authorization code to redeem.</param>
        /// <param name="redirectUri">Uri that was passed in the request sent for the authorization code.</param>
        /// <returns>OpenIdConnect message that has tokens inside it.</returns>
        protected virtual async Task<OpenIdConnectMessage> RedeemAuthorizationCodeAsync(string authorizationCode, string redirectUri)
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
            return new OpenIdConnectMessage(jsonTokenResponse);
        }

        /// <summary>
        /// Goes to UserInfo endpoint to retrieve additional claims and add any unique claims to the given identity.
        /// </summary>
        /// <param name="message">message that is being processed</param>
        /// <param name="ticket">authentication ticket with claims principal and identities</param>
        /// <returns>Authentication ticket with identity with additional claims, if any.</returns>
        protected virtual async Task<AuthenticationTicket> GetUserInformationAsync(OpenIdConnectMessage message, JwtSecurityToken jwt, AuthenticationTicket ticket)
        {
            var userInfoEndpoint = _configuration?.UserInfoEndpoint;

            if (string.IsNullOrEmpty(userInfoEndpoint))
            {
                Logger.LogWarning(24, nameof(_configuration.UserInfoEndpoint) + " is not set. Request to retrieve claims cannot be completed.");
                return ticket;
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", message.AccessToken);
            var responseMessage = await Backchannel.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();
            var userInfoResponse = await responseMessage.Content.ReadAsStringAsync();
            JObject user;
            var contentType = responseMessage.Content.Headers.ContentType;
            if (contentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                user = JObject.Parse(userInfoResponse);
            }
            else if (contentType.MediaType.Equals("application/jwt", StringComparison.OrdinalIgnoreCase))
            {
                var userInfoEndpointJwt = new JwtSecurityToken(userInfoResponse);
                user = JObject.FromObject(userInfoEndpointJwt.Payload);
            }
            else
            {
                throw new NotSupportedException("Unknown response type: " + contentType.MediaType);
            }

            var userInformationReceivedContext = await RunUserInformationReceivedEventAsync(ticket, message, user);
            if (userInformationReceivedContext.HandledResponse)
            {
                return userInformationReceivedContext.AuthenticationTicket;
            }
            else if (userInformationReceivedContext.Skipped)
            {
                return null;
            }
            ticket = userInformationReceivedContext.AuthenticationTicket;
            user = userInformationReceivedContext.User;

            Options.ProtocolValidator.ValidateUserInfoResponse(new OpenIdConnectProtocolValidationContext()
            {
                UserInfoEndpointResponse = userInfoResponse,
                ValidatedIdToken = jwt,
            });

            var identity = (ClaimsIdentity)ticket.Principal.Identity;

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

            return ticket;
        }

        /// <summary>
        /// Save the tokens contained in the <see cref="OpenIdConnectMessage"/> in the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="principal">The principal in which tokens are saved.</param>
        /// <param name="message">The OpenID Connect response.</param>
        /// <param name="saveRefreshToken">A <see cref="bool"/> indicating whether the refresh token should be stored.</param>
        private void SaveTokens(ClaimsPrincipal principal, OpenIdConnectMessage message, bool saveRefreshToken)
        {
            var identity = (ClaimsIdentity)principal.Identity;

            if (!string.IsNullOrEmpty(message.AccessToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.AccessToken, message.AccessToken,
                                            ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            if (!string.IsNullOrEmpty(message.IdToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.IdToken, message.IdToken,
                                            ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            if (saveRefreshToken && !string.IsNullOrEmpty(message.RefreshToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.RefreshToken, message.RefreshToken,
                                            ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            if (!string.IsNullOrEmpty(message.TokenType))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.TokenType, message.TokenType,
                                            ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            if (!string.IsNullOrEmpty(message.ExpiresIn))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.ExpiresIn, message.ExpiresIn,
                                            ClaimValueTypes.String, Options.ClaimsIssuer));
            }
        }

        /// <summary>
        /// Adds the nonce to <see cref="HttpResponse.Cookies"/>.
        /// </summary>
        /// <param name="nonce">the nonce to remember.</param>
        /// <remarks><see cref="HttpResponse.Cookies.Append"/>is called to add a cookie with the name: 'OpenIdConnectAuthenticationDefaults.Nonce + <see cref="OpenIdConnectOptions.StringDataFormat.Protect"/>(nonce)'.
        /// The value of the cookie is: "N".</remarks>
        private void WriteNonceCookie(string nonce)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                throw new ArgumentNullException(nameof(nonce));
            }

            Response.Cookies.Append(
                OpenIdConnectDefaults.CookieNoncePrefix + Options.StringDataFormat.Protect(nonce),
                NonceProperty,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    Expires = DateTime.UtcNow + Options.ProtocolValidator.NonceLifetime
                });
        }

        /// <summary>
        /// Searches <see cref="HttpRequest.Cookies"/> for a matching nonce.
        /// </summary>
        /// <param name="nonce">the nonce that we are looking for.</param>
        /// <returns>echos 'nonce' if a cookie is found that matches, null otherwise.</returns>
        /// <remarks>Examine <see cref="HttpRequest.Cookies.Keys"/> that start with the prefix: 'OpenIdConnectAuthenticationDefaults.Nonce'. 
        /// <see cref="OpenIdConnectOptions.StringDataFormat.Unprotect"/> is used to obtain the actual 'nonce'. If the nonce is found, then <see cref="HttpResponse.Cookies.Delete"/> is called.</remarks>
        private string ReadNonceCookie(string nonce)
        {
            if (nonce == null)
            {
                return null;
            }

            foreach (var nonceKey in Request.Cookies.Keys)
            {
                if (nonceKey.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix))
                {
                    try
                    {
                        var nonceDecodedValue = Options.StringDataFormat.Unprotect(nonceKey.Substring(OpenIdConnectDefaults.CookieNoncePrefix.Length, nonceKey.Length - OpenIdConnectDefaults.CookieNoncePrefix.Length));
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
                        Logger.LogWarning(25, "Failed to un-protect the nonce cookie.", ex);
                    }
                }
            }

            return null;
        }

        private void GenerateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var correlationKey = OpenIdConnectDefaults.CookieStatePrefix;

            var nonceBytes = new byte[32];
            CryptoRandom.GetBytes(nonceBytes);
            var correlationId = Base64UrlTextEncoder.Encode(nonceBytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                Expires = DateTime.UtcNow + Options.ProtocolValidator.NonceLifetime
            };

            properties.Items[correlationKey] = correlationId;

            Response.Cookies.Append(correlationKey + correlationId, NonceProperty, cookieOptions);
        }

        private bool ValidateCorrelationId(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var correlationKey = OpenIdConnectDefaults.CookieStatePrefix;

            string correlationId;
            if (!properties.Items.TryGetValue(
                correlationKey,
                out correlationId))
            {
                Logger.LogWarning(26, "{0} state property not found.", correlationKey);
                return false;
            }

            properties.Items.Remove(correlationKey);

            var cookieName = correlationKey + correlationId;

            var correlationCookie = Request.Cookies[cookieName];
            if (string.IsNullOrEmpty(correlationCookie))
            {
                Logger.LogWarning(27, "{0} cookie not found.", cookieName);
                return false;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };
            Response.Cookies.Delete(cookieName, cookieOptions);

            if (!string.Equals(correlationCookie, NonceProperty, StringComparison.Ordinal))
            {
                Logger.LogWarning(28, "{0} correlation cookie and state property mismatch.", correlationKey);
                return false;
            }

            return true;
        }

        private AuthenticationProperties GetPropertiesFromState(string state)
        {
            // assume a well formed query string: <a=b&>OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey=kasjd;fljasldkjflksdj<&c=d>
            var startIndex = 0;
            if (string.IsNullOrEmpty(state) || (startIndex = state.IndexOf(OpenIdConnectDefaults.AuthenticationPropertiesKey, StringComparison.Ordinal)) == -1)
            {
                return null;
            }

            var authenticationIndex = startIndex + OpenIdConnectDefaults.AuthenticationPropertiesKey.Length;
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

        private async Task<MessageReceivedContext> RunMessageReceivedEventAsync(OpenIdConnectMessage message)
        {
            Logger.LogDebug(29, "MessageReceived: '{0}'", message.BuildRedirectUrl());
            var messageReceivedContext = new MessageReceivedContext(Context, Options)
            {
                ProtocolMessage = message
            };

            await Options.Events.MessageReceived(messageReceivedContext);
            if (messageReceivedContext.HandledResponse)
            {
                Logger.LogVerbose(30, "MessageReceivedContext.HandledResponse");
            }
            else if (messageReceivedContext.Skipped)
            {
                Logger.LogVerbose(31, "MessageReceivedContext.Skipped");
            }

            return messageReceivedContext;
        }

        private async Task<AuthorizationCodeReceivedContext> RunAuthorizationCodeReceivedEventAsync(OpenIdConnectMessage message, AuthenticationProperties properties, AuthenticationTicket ticket, JwtSecurityToken jwt)
        {
            var redirectUri = properties.Items.ContainsKey(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey) ?
                properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] : Options.RedirectUri;

            Logger.LogDebug(32, "AuthorizationCode received: '{0}'", message.Code);

            var authorizationCodeReceivedContext = new AuthorizationCodeReceivedContext(Context, Options)
            {
                Code = message.Code,
                ProtocolMessage = message,
                RedirectUri = redirectUri,
                AuthenticationTicket = ticket,
                JwtSecurityToken = jwt
            };

            await Options.Events.AuthorizationCodeReceived(authorizationCodeReceivedContext);
            if (authorizationCodeReceivedContext.HandledResponse)
            {
                Logger.LogVerbose(33, "AuthorizationCodeReceivedContext.HandledResponse");
            }
            else if (authorizationCodeReceivedContext.Skipped)
            {
                Logger.LogVerbose(34, "AuthorizationCodeReceivedContext.Skipped");
            }

            return authorizationCodeReceivedContext;
        }

        private async Task<TokenResponseReceivedContext> RunTokenResponseReceivedEventAsync(OpenIdConnectMessage message, OpenIdConnectMessage tokenEndpointResponse)
        {
            Logger.LogDebug(35, "Token response received.");
            var tokenResponseReceivedContext = new TokenResponseReceivedContext(Context, Options)
            {
                ProtocolMessage = message,
                TokenEndpointResponse = tokenEndpointResponse
            };

            await Options.Events.TokenResponseReceived(tokenResponseReceivedContext);
            if (tokenResponseReceivedContext.HandledResponse)
            {
                Logger.LogVerbose(36, "AuthorizationCodeRedeemedContext.HandledResponse");
            }
            else if (tokenResponseReceivedContext.Skipped)
            {
                Logger.LogVerbose(37, "AuthorizationCodeRedeemedContext.Skipped");
            }
            return tokenResponseReceivedContext;
        }

        private async Task<AuthenticationValidatedContext> RunAuthenticationValidatedEventAsync(OpenIdConnectMessage message, AuthenticationTicket ticket, OpenIdConnectMessage tokenEndpointResponse)
        {
            var authenticationValidatedContext = new AuthenticationValidatedContext(Context, Options)
            {
                AuthenticationTicket = ticket,
                ProtocolMessage = message,
                TokenEndpointResponse = tokenEndpointResponse,
            };

            await Options.Events.AuthenticationValidated(authenticationValidatedContext);
            if (authenticationValidatedContext.HandledResponse)
            {
                Logger.LogVerbose(38, "AuthenticationValidated.HandledResponse");
            }
            else if (authenticationValidatedContext.Skipped)
            {
                Logger.LogVerbose(39, "AuthenticationValidated.Skipped");
            }

            return authenticationValidatedContext;
        }

        private async Task<UserInformationReceivedContext> RunUserInformationReceivedEventAsync(AuthenticationTicket ticket, OpenIdConnectMessage message, JObject user)
        {
            Logger.LogDebug(40, "User information received: {0}", user.ToString());

            var userInformationReceivedContext = new UserInformationReceivedContext(Context, Options)
            {
                AuthenticationTicket = ticket,
                ProtocolMessage = message,
                User = user,
            };

            await Options.Events.UserInformationReceived(userInformationReceivedContext);
            if (userInformationReceivedContext.HandledResponse)
            {
                Logger.LogVerbose(41, "The UserInformationReceived event returned Handled.");
            }
            else if (userInformationReceivedContext.Skipped)
            {
                Logger.LogVerbose(42, "The UserInformationReceived event returned Skipped.");
            }

            return userInformationReceivedContext;
        }

        private async Task<AuthenticationFailedContext> RunAuthenticationFailedEventAsync(OpenIdConnectMessage message, Exception exception)
        {
            var authenticationFailedContext = new AuthenticationFailedContext(Context, Options)
            {
                ProtocolMessage = message,
                Exception = exception
            };

            await Options.Events.AuthenticationFailed(authenticationFailedContext);
            if (authenticationFailedContext.HandledResponse)
            {
                Logger.LogVerbose(43, "AuthenticationFailedContext.HandledResponse");
            }
            else if (authenticationFailedContext.Skipped)
            {
                Logger.LogVerbose(44, "AuthenticationFailedContext.Skipped");
            }

            return authenticationFailedContext;
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
            if (Options.SecurityTokenValidator.CanReadToken(idToken))
            {
                principal = Options.SecurityTokenValidator.ValidateToken(idToken, validationParameters, out validatedToken);
                jwt = validatedToken as JwtSecurityToken;
                if (jwt == null)
                {
                    Logger.LogError(45, "The Validated Security Token must be of type JwtSecurityToken, but instead its type is: '{0}'", validatedToken?.GetType());
                    throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.ValidatedSecurityTokenNotJwt, validatedToken?.GetType()));
                }
            }

            if (validatedToken == null)
            {
                Logger.LogError(46, "Unable to validate the 'id_token', no suitable ISecurityTokenValidator was found for: '{0}'.", idToken);
                throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToValidateToken, idToken));
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
    }
}
