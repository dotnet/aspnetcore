// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
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

        protected HtmlEncoder HtmlEncoder { get; private set; }

        public OpenIdConnectHandler(HttpClient backchannel, HtmlEncoder htmlEncoder)
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
                var logoutRedirectUri = properties.RedirectUri;
                if (!string.IsNullOrEmpty(logoutRedirectUri))
                {
                    // Relative to PathBase
                    if (logoutRedirectUri.StartsWith("/", StringComparison.Ordinal))
                    {
                        logoutRedirectUri = BuildRedirectUri(logoutRedirectUri);
                    }
                    message.PostLogoutRedirectUri = logoutRedirectUri;
                }
                else if (!string.IsNullOrEmpty(Options.PostLogoutRedirectUri))
                {
                    logoutRedirectUri = Options.PostLogoutRedirectUri;
                    // Relative to PathBase
                    if (logoutRedirectUri.StartsWith("/", StringComparison.Ordinal))
                    {
                        logoutRedirectUri = BuildRedirectUri(logoutRedirectUri);
                    }
                    message.PostLogoutRedirectUri = logoutRedirectUri;
                }

                var principal = await Context.Authentication.AuthenticateAsync(Options.SignInScheme);
                message.IdTokenHint = principal?.FindFirst(OpenIdConnectParameterNames.IdToken)?.Value;

                var redirectContext = new RedirectContext(Context, Options, properties)
                {
                    ProtocolMessage = message
                };

                await Options.Events.RedirectToEndSessionEndpoint(redirectContext);
                if (redirectContext.HandledResponse)
                {
                    Logger.LogDebug(1, "RedirectToEndSessionEndpoint.HandledResponse");
                    return;
                }
                else if (redirectContext.Skipped)
                {
                    Logger.LogDebug(2, "RedirectToEndSessionEndpoint.Skipped");
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
                        var name = HtmlEncoder.Encode(parameter.Key);
                        var value = HtmlEncoder.Encode(parameter.Value);

                        var input = string.Format(CultureInfo.InvariantCulture, InputTagFormat, name, value);
                        inputs.AppendLine(input);
                    }

                    var issuer = HtmlEncoder.Encode(message.IssuerAddress);

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

            Logger.LogTrace(4, "Entering {0}." + nameof(HandleUnauthorizedAsync), GetType());

            // order for local RedirectUri
            // 1. challenge.Properties.RedirectUri
            // 2. CurrentUri if RedirectUri is not set)
            var properties = new AuthenticationProperties(context.Properties)
            {
                ExpiresUtc = Options.SystemClock.UtcNow.Add(Options.RemoteAuthenticationTimeout)
            };

            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }
            Logger.LogTrace(5, "Using properties.RedirectUri for 'local redirect' post authentication: '{0}'.", properties.RedirectUri);

            if (_configuration == null && Options.ConfigurationManager != null)
            {
                _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
            }

            var message = new OpenIdConnectMessage
            {
                ClientId = Options.ClientId,
                IssuerAddress = _configuration?.AuthorizationEndpoint ?? string.Empty,
                RedirectUri = BuildRedirectUri(Options.CallbackPath),
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

            var redirectContext = new RedirectContext(Context, Options, properties)
            {
                ProtocolMessage = message
            };

            await Options.Events.RedirectToAuthenticationEndpoint(redirectContext);
            if (redirectContext.HandledResponse)
            {
                Logger.LogDebug(6, "RedirectToAuthenticationEndpoint.HandledResponse");
                return true;
            }
            else if (redirectContext.Skipped)
            {
                Logger.LogDebug(7, "RedirectToAuthenticationEndpoint.Skipped");
                return false;
            }

            message = redirectContext.ProtocolMessage;

            if (!string.IsNullOrEmpty(message.State))
            {
                properties.Items[OpenIdConnectDefaults.UserstatePropertiesKey] = message.State;
            }

            // When redeeming a 'code' for an AccessToken, this value is needed
            properties.Items.Add(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, message.RedirectUri);

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
                    var name = HtmlEncoder.Encode(parameter.Key);
                    var value = HtmlEncoder.Encode(parameter.Value);

                    var input = string.Format(CultureInfo.InvariantCulture, InputTagFormat, name, value);
                    inputs.AppendLine(input);
                }

                var issuer = HtmlEncoder.Encode(message.IssuerAddress);

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
            Logger.LogTrace(10, "Entering: {0}." + nameof(HandleRemoteAuthenticateAsync), GetType());

            OpenIdConnectMessage authorizationResponse = null;

            if (string.Equals(Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                authorizationResponse = new OpenIdConnectMessage(Request.Query.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));

                // response_mode=query (explicit or not) and a response_type containing id_token
                // or token are not considered as a safe combination and MUST be rejected.
                // See http://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#Security
                if (!string.IsNullOrEmpty(authorizationResponse.IdToken) || !string.IsNullOrEmpty(authorizationResponse.AccessToken))
                {
                    if (Options.SkipUnrecognizedRequests)
                    {
                        // Not for us?
                        return AuthenticateResult.Skip();
                    }
                    return AuthenticateResult.Fail("An OpenID Connect response cannot contain an " +
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
                authorizationResponse = new OpenIdConnectMessage(form.Select(pair => new KeyValuePair<string, string[]>(pair.Key, pair.Value)));
            }

            if (authorizationResponse == null)
            {
                if (Options.SkipUnrecognizedRequests)
                {
                    // Not for us?
                    return AuthenticateResult.Skip();
                }
                return AuthenticateResult.Fail("No message.");
            }

            AuthenticateResult result;

            try
            {
                var messageReceivedContext = await RunMessageReceivedEventAsync(authorizationResponse);
                if (CheckEventResult(messageReceivedContext, out result))
                {
                    return result;
                }
                authorizationResponse = messageReceivedContext.ProtocolMessage;

                // Fail if state is missing, it's required for the correlation id.
                if (string.IsNullOrEmpty(authorizationResponse.State))
                {
                    // This wasn't a valid OIDC message, it may not have been intended for us.
                    Logger.LogDebug(11, "message.State is null or empty.");
                    if (Options.SkipUnrecognizedRequests)
                    {
                        return AuthenticateResult.Skip();
                    }
                    return AuthenticateResult.Fail(Resources.MessageStateIsNullOrEmpty);
                }

                // if state exists and we failed to 'unprotect' this is not a message we should process.
                var properties = Options.StateDataFormat.Unprotect(Uri.UnescapeDataString(authorizationResponse.State));
                if (properties == null)
                {
                    Logger.LogDebug(12, "Unable to read the message.State.");
                    if (Options.SkipUnrecognizedRequests)
                    {
                        // Not for us?
                        return AuthenticateResult.Skip();
                    }
                    return AuthenticateResult.Fail(Resources.MessageStateIsInvalid);
                }

                // if any of the error fields are set, throw error null
                if (!string.IsNullOrEmpty(authorizationResponse.Error))
                {
                    Logger.LogError(13, "Message contains error: '{0}', error_description: '{1}', error_uri: '{2}'.", authorizationResponse.Error, authorizationResponse.ErrorDescription ?? "ErrorDecription null", authorizationResponse.ErrorUri ?? "ErrorUri null");
                    return AuthenticateResult.Fail(new OpenIdConnectProtocolException(string.Format(CultureInfo.InvariantCulture, Resources.MessageContainsError, authorizationResponse.Error, authorizationResponse.ErrorDescription ?? "ErrorDecription null", authorizationResponse.ErrorUri ?? "ErrorUri null")));
                }

                string userstate = null;
                properties.Items.TryGetValue(OpenIdConnectDefaults.UserstatePropertiesKey, out userstate);
                authorizationResponse.State = userstate;

                if (!ValidateCorrelationId(properties))
                {
                    return AuthenticateResult.Fail("Correlation failed.");
                }

                if (_configuration == null && Options.ConfigurationManager != null)
                {
                    Logger.LogDebug(14, "Updating configuration");
                    _configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                }

                var authorizationResponseReceivedContext = await RunAuthorizationResponseReceivedEventAsync(authorizationResponse, properties);
                if (CheckEventResult(authorizationResponseReceivedContext, out result))
                {
                    return result;
                }
                authorizationResponse = authorizationResponseReceivedContext.ProtocolMessage;
                properties = authorizationResponseReceivedContext.Properties;

                PopulateSessionProperties(authorizationResponse, properties);

                AuthenticationTicket ticket = null;
                JwtSecurityToken jwt = null;
                string nonce = null;
                var validationParameters = Options.TokenValidationParameters.Clone();

                // Hybrid or Implicit flow
                if (!string.IsNullOrEmpty(authorizationResponse.IdToken))
                {
                    Logger.LogDebug(23, "'id_token' received.");
                    ticket = ValidateToken(authorizationResponse.IdToken, properties, validationParameters, out jwt);

                    nonce = jwt?.Payload.Nonce;
                    if (!string.IsNullOrEmpty(nonce))
                    {
                        nonce = ReadNonceCookie(nonce);
                    }
                }

                Options.ProtocolValidator.ValidateAuthenticationResponse(new OpenIdConnectProtocolValidationContext()
                {
                    ClientId = Options.ClientId,
                    ProtocolMessage = authorizationResponse,
                    ValidatedIdToken = jwt,
                    Nonce = nonce
                });

                // TODO: AuthorizationResponseValidated event?

                OpenIdConnectMessage tokenEndpointResponse = null;

                // Authorization Code or Hybrid flow
                if (!string.IsNullOrEmpty(authorizationResponse.Code))
                {
                    // TODO: Does this event provide any value over AuthorizationResponseReceived or AuthorizationResponseValidated?
                    var authorizationCodeReceivedContext = await RunAuthorizationCodeReceivedEventAsync(authorizationResponse, properties, ticket, jwt);
                    if (CheckEventResult(authorizationCodeReceivedContext, out result))
                    {
                        return result;
                    }
                    authorizationResponse = authorizationCodeReceivedContext.ProtocolMessage;
                    var code = authorizationCodeReceivedContext.Code;

                    tokenEndpointResponse = await RedeemAuthorizationCodeAsync(code, authorizationCodeReceivedContext.RedirectUri);

                    var authorizationCodeRedeemedContext = await RunTokenResponseReceivedEventAsync(authorizationResponse, tokenEndpointResponse, properties);
                    if (CheckEventResult(authorizationCodeRedeemedContext, out result))
                    {
                        return result;
                    }
                    authorizationResponse = authorizationCodeRedeemedContext.ProtocolMessage;
                    tokenEndpointResponse = authorizationCodeRedeemedContext.TokenEndpointResponse;

                    // We only have to process the IdToken if we didn't already get one in the AuthorizationResponse
                    if (ticket == null)
                    {
                        // no need to validate signature when token is received using "code flow" as per spec
                        // [http://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation].
                        validationParameters.RequireSignedTokens = false;

                        ticket = ValidateToken(tokenEndpointResponse.IdToken, properties, validationParameters, out jwt);

                        nonce = jwt?.Payload.Nonce;
                        if (!string.IsNullOrEmpty(nonce))
                        {
                            nonce = ReadNonceCookie(nonce);
                        }
                    }

                    Options.ProtocolValidator.ValidateTokenResponse(new OpenIdConnectProtocolValidationContext()
                    {
                        ClientId = Options.ClientId,
                        ProtocolMessage = tokenEndpointResponse,
                        ValidatedIdToken = jwt,
                        Nonce = nonce
                    });
                }

                var authenticationValidatedContext = await RunAuthenticationValidatedEventAsync(authorizationResponse, ticket, properties, tokenEndpointResponse);
                if (CheckEventResult(authenticationValidatedContext, out result))
                {
                    return result;
                }
                authorizationResponse = authenticationValidatedContext.ProtocolMessage;
                tokenEndpointResponse = authenticationValidatedContext.TokenEndpointResponse;
                ticket = authenticationValidatedContext.Ticket;

                if (Options.SaveTokensAsClaims)
                {
                    SaveTokens(ticket.Principal, tokenEndpointResponse ?? authorizationResponse, jwt.Issuer);
                }

                if (Options.GetClaimsFromUserInfoEndpoint)
                {
                    return await GetUserInformationAsync(tokenEndpointResponse ?? authorizationResponse, jwt, ticket);
                }

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception exception)
            {
                Logger.LogError(19, "Exception occurred while processing message.", exception);

                // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
                if (Options.RefreshOnIssuerKeyNotFound && exception.GetType().Equals(typeof(SecurityTokenSignatureKeyNotFoundException)))
                {
                    if (Options.ConfigurationManager != null)
                    {
                        Logger.LogDebug(20, "exception of type 'SecurityTokenSignatureKeyNotFoundException' thrown, Options.ConfigurationManager.RequestRefresh() called.");
                        Options.ConfigurationManager.RequestRefresh();
                    }
                }

                var authenticationFailedContext = await RunAuthenticationFailedEventAsync(authorizationResponse, exception);
                if (CheckEventResult(authenticationFailedContext, out result))
                {
                    return result;
                }

                return AuthenticateResult.Fail(exception);
            }
        }

        private bool CheckEventResult(BaseControlContext context, out AuthenticateResult result)
        {
            if (context.HandledResponse)
            {
                result = AuthenticateResult.Success(context.Ticket);
                return true;
            }
            else if (context.Skipped)
            {
                result = AuthenticateResult.Skip();
                return true;
            }
            result = null;
            return false;
        }

        private void PopulateSessionProperties(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            if (!string.IsNullOrEmpty(message.SessionState))
            {
                properties.Items[OpenIdConnectSessionProperties.SessionState] = message.SessionState;
            }

            if (!string.IsNullOrEmpty(_configuration.CheckSessionIframe))
            {
                properties.Items[OpenIdConnectSessionProperties.CheckSessionIFrame] = _configuration.CheckSessionIframe;
            }
        }

        /// <summary>
        /// Redeems the authorization code for tokens at the token endpoint
        /// </summary>
        /// <param name="authorizationCode">The authorization code to redeem.</param>
        /// <param name="redirectUri">Uri that was passed in the request sent for the authorization code.</param>
        /// <returns>OpenIdConnect message that has tokens inside it.</returns>
        protected virtual async Task<OpenIdConnectMessage> RedeemAuthorizationCodeAsync(string authorizationCode, string redirectUri)
        {
            Logger.LogDebug(21, "Redeeming code for tokens.");

            var openIdMessage = new OpenIdConnectMessage()
            {
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                Code = authorizationCode,
                GrantType = "authorization_code",
                RedirectUri = redirectUri
            };

            // TODO: Event that lets you customize the message. E.g. use certificates, specify resources.

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
        protected virtual async Task<AuthenticateResult> GetUserInformationAsync(OpenIdConnectMessage message, JwtSecurityToken jwt, AuthenticationTicket ticket)
        {
            var userInfoEndpoint = _configuration?.UserInfoEndpoint;

            if (string.IsNullOrEmpty(userInfoEndpoint))
            {
                Logger.LogDebug(24, $"{nameof(_configuration.UserInfoEndpoint)} is not set. Claims cannot be retrieved.");
                return AuthenticateResult.Success(ticket);
            }
            if (string.IsNullOrEmpty(message.AccessToken))
            {
                Logger.LogDebug(47, "The access_token is not available. Claims cannot be retrieved.");
                return AuthenticateResult.Success(ticket);
            }
            Logger.LogTrace(22, "Retrieving claims from the user info endpoint.");

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
                return AuthenticateResult.Fail("Unknown response type: " + contentType.MediaType);
            }

            var userInformationReceivedContext = await RunUserInformationReceivedEventAsync(ticket, message, user);
            AuthenticateResult result;
            if (CheckEventResult(userInformationReceivedContext, out result))
            {
                return result;
            }
            ticket = userInformationReceivedContext.Ticket;
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
                identity.AddClaim(new Claim(pair.Key, claimValue, ClaimValueTypes.String, jwt.Issuer));
            }

            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Save the tokens contained in the <see cref="OpenIdConnectMessage"/> in the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="principal">The principal in which tokens are saved.</param>
        /// <param name="message">The OpenID Connect response.</param>
        private void SaveTokens(ClaimsPrincipal principal, OpenIdConnectMessage message, string issuer)
        {
            var identity = (ClaimsIdentity)principal.Identity;

            if (!string.IsNullOrEmpty(message.AccessToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.AccessToken, message.AccessToken,
                                            ClaimValueTypes.String, issuer));
            }

            if (!string.IsNullOrEmpty(message.IdToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.IdToken, message.IdToken,
                                            ClaimValueTypes.String, issuer));
            }

            if (!string.IsNullOrEmpty(message.RefreshToken))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.RefreshToken, message.RefreshToken,
                                            ClaimValueTypes.String, issuer));
            }

            if (!string.IsNullOrEmpty(message.TokenType))
            {
                identity.AddClaim(new Claim(OpenIdConnectParameterNames.TokenType, message.TokenType,
                                            ClaimValueTypes.String, issuer));
            }

            if (!string.IsNullOrEmpty(message.ExpiresIn))
            {
                int value;
                if (int.TryParse(message.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    var expiresAt = Options.SystemClock.UtcNow + TimeSpan.FromSeconds(value);
                    // https://www.w3.org/TR/xmlschema-2/#dateTime
                    // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx
                    identity.AddClaim(new Claim("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture),
                                                ClaimValueTypes.DateTime, issuer));
                }
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
                    Expires = Options.SystemClock.UtcNow.Add(Options.ProtocolValidator.NonceLifetime)
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
            Logger.LogTrace(29, "MessageReceived: '{0}'", message.BuildRedirectUrl());
            var messageReceivedContext = new MessageReceivedContext(Context, Options)
            {
                ProtocolMessage = message
            };

            await Options.Events.MessageReceived(messageReceivedContext);
            if (messageReceivedContext.HandledResponse)
            {
                Logger.LogDebug(30, "MessageReceivedContext.HandledResponse");
            }
            else if (messageReceivedContext.Skipped)
            {
                Logger.LogDebug(31, "MessageReceivedContext.Skipped");
            }

            return messageReceivedContext;
        }

        private async Task<AuthorizationResponseReceivedContext> RunAuthorizationResponseReceivedEventAsync(OpenIdConnectMessage message, AuthenticationProperties properties)
        {
            Logger.LogTrace(15, "Authorization response received.");
            var authorizationResponseReceivedContext = new AuthorizationResponseReceivedContext(Context, Options, properties)
            {
                ProtocolMessage = message
            };
            await Options.Events.AuthorizationResponseReceived(authorizationResponseReceivedContext);
            if (authorizationResponseReceivedContext.HandledResponse)
            {
                Logger.LogDebug(16, "AuthorizationResponseReceived.HandledResponse");
            }
            else if (authorizationResponseReceivedContext.Skipped)
            {
                Logger.LogDebug(17, "AuthorizationResponseReceived.Skipped");
            }
            return authorizationResponseReceivedContext;
        }

        private async Task<AuthorizationCodeReceivedContext> RunAuthorizationCodeReceivedEventAsync(OpenIdConnectMessage message, AuthenticationProperties properties, AuthenticationTicket ticket, JwtSecurityToken jwt)
        {
            var redirectUri = properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey];

            Logger.LogTrace(32, "AuthorizationCode received: '{0}'", message.Code);

            var authorizationCodeReceivedContext = new AuthorizationCodeReceivedContext(Context, Options, properties)
            {
                Code = message.Code,
                ProtocolMessage = message,
                RedirectUri = redirectUri,
                Ticket = ticket,
                JwtSecurityToken = jwt
            };

            await Options.Events.AuthorizationCodeReceived(authorizationCodeReceivedContext);
            if (authorizationCodeReceivedContext.HandledResponse)
            {
                Logger.LogDebug(33, "AuthorizationCodeReceivedContext.HandledResponse");
            }
            else if (authorizationCodeReceivedContext.Skipped)
            {
                Logger.LogDebug(34, "AuthorizationCodeReceivedContext.Skipped");
            }

            return authorizationCodeReceivedContext;
        }

        private async Task<TokenResponseReceivedContext> RunTokenResponseReceivedEventAsync(OpenIdConnectMessage message, OpenIdConnectMessage tokenEndpointResponse, AuthenticationProperties properties)
        {
            Logger.LogTrace(35, "Token response received.");
            var tokenResponseReceivedContext = new TokenResponseReceivedContext(Context, Options, properties)
            {
                ProtocolMessage = message,
                TokenEndpointResponse = tokenEndpointResponse
            };

            await Options.Events.TokenResponseReceived(tokenResponseReceivedContext);
            if (tokenResponseReceivedContext.HandledResponse)
            {
                Logger.LogDebug(36, "AuthorizationCodeRedeemedContext.HandledResponse");
            }
            else if (tokenResponseReceivedContext.Skipped)
            {
                Logger.LogDebug(37, "AuthorizationCodeRedeemedContext.Skipped");
            }
            return tokenResponseReceivedContext;
        }

        private async Task<AuthenticationValidatedContext> RunAuthenticationValidatedEventAsync(OpenIdConnectMessage message, AuthenticationTicket ticket, AuthenticationProperties properties, OpenIdConnectMessage tokenResponse)
        {
            var authenticationValidatedContext = new AuthenticationValidatedContext(Context, Options, properties)
            {
                Ticket = ticket,
                ProtocolMessage = message,
                TokenEndpointResponse = tokenResponse,
            };

            await Options.Events.AuthenticationValidated(authenticationValidatedContext);
            if (authenticationValidatedContext.HandledResponse)
            {
                Logger.LogDebug(38, "AuthenticationValidated.HandledResponse");
            }
            else if (authenticationValidatedContext.Skipped)
            {
                Logger.LogDebug(39, "AuthenticationValidated.Skipped");
            }

            return authenticationValidatedContext;
        }

        private async Task<UserInformationReceivedContext> RunUserInformationReceivedEventAsync(AuthenticationTicket ticket, OpenIdConnectMessage message, JObject user)
        {
            Logger.LogTrace(40, "User information received: {0}", user.ToString());

            var userInformationReceivedContext = new UserInformationReceivedContext(Context, Options)
            {
                Ticket = ticket,
                ProtocolMessage = message,
                User = user,
            };

            await Options.Events.UserInformationReceived(userInformationReceivedContext);
            if (userInformationReceivedContext.HandledResponse)
            {
                Logger.LogDebug(41, "The UserInformationReceived event returned Handled.");
            }
            else if (userInformationReceivedContext.Skipped)
            {
                Logger.LogDebug(42, "The UserInformationReceived event returned Skipped.");
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
                Logger.LogDebug(43, "AuthenticationFailedContext.HandledResponse");
            }
            else if (authenticationFailedContext.Skipped)
            {
                Logger.LogDebug(44, "AuthenticationFailedContext.Skipped");
            }

            return authenticationFailedContext;
        }

        private AuthenticationTicket ValidateToken(string idToken, AuthenticationProperties properties, TokenValidationParameters validationParameters, out JwtSecurityToken jwt)
        {
            if (!Options.SecurityTokenValidator.CanReadToken(idToken))
            {
                Logger.LogError(48, "Unable to read the 'id_token', no suitable ISecurityTokenValidator was found for: '{0}'.", idToken);
                throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToValidateToken, idToken));
            }

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
            var principal = Options.SecurityTokenValidator.ValidateToken(idToken, validationParameters, out validatedToken);
            jwt = validatedToken as JwtSecurityToken;
            if (jwt == null)
            {
                Logger.LogError(45, "The Validated Security Token must be of type JwtSecurityToken, but instead its type is: '{0}'", validatedToken?.GetType());
                throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.ValidatedSecurityTokenNotJwt, validatedToken?.GetType()));
            }

            if (validatedToken == null)
            {
                Logger.LogError(46, "Unable to validate the 'id_token', no suitable ISecurityTokenValidator was found for: '{0}'.", idToken);
                throw new SecurityTokenException(string.Format(CultureInfo.InvariantCulture, Resources.UnableToValidateToken, idToken));
            }

            var ticket = new AuthenticationTicket(principal, properties, Options.AuthenticationScheme);

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
