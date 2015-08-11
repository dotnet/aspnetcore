// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OAuth
{
    public class OAuthAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions> where TOptions : OAuthAuthenticationOptions
    {
        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

        public OAuthAuthenticationHandler(HttpClient backchannel)
        {
            Backchannel = backchannel;
        }

        protected HttpClient Backchannel { get; private set; }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync();
            }
            return false;
        }

        public async Task<bool> InvokeReturnPathAsync()
        {
            var ticket = await HandleAuthenticateOnceAsync();
            if (ticket == null)
            {
                Logger.LogWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new OAuthReturnEndpointContext(Context, ticket)
            {
                SignInScheme = Options.SignInScheme,
                RedirectUri = ticket.Properties.RedirectUri,
            };
            ticket.Properties.RedirectUri = null;

            await Options.Notifications.ReturnEndpoint(context);

            if (context.SignInScheme != null && context.Principal != null)
            {
                await Context.Authentication.SignInAsync(context.SignInScheme, context.Principal, context.Properties);
            }

            if (!context.IsRequestCompleted && context.RedirectUri != null)
            {
                if (context.Principal == null)
                {
                    // add a redirect hint that sign-in failed in some way
                    context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "error", "access_denied");
                }
                Response.Redirect(context.RedirectUri);
                context.RequestCompleted();
            }

            return context.IsRequestCompleted;
        }

        protected override async Task<AuthenticationTicket> HandleAuthenticateAsync()
        {
            AuthenticationProperties properties = null;
            try
            {
                var query = Request.Query;

                // TODO: Is this a standard error returned by servers?
                var value = query.Get("error");
                if (!string.IsNullOrEmpty(value))
                {
                    Logger.LogVerbose("Remote server returned an error: " + Request.QueryString);
                    // TODO: Fail request rather than passing through?
                    return null;
                }

                var code = query.Get("code");
                var state = query.Get("state");

                properties = Options.StateDataFormat.Unprotect(state);
                if (properties == null)
                {
                    return null;
                }

                // OAuth2 10.12 CSRF
                if (!ValidateCorrelationId(properties))
                {
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                if (string.IsNullOrEmpty(code))
                {
                    // Null if the remote server returns an error.
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                var tokens = await ExchangeCodeAsync(code, BuildRedirectUri(Options.CallbackPath));

                if (string.IsNullOrEmpty(tokens.AccessToken))
                {
                    Logger.LogWarning("Access token was not found");
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                var identity = new ClaimsIdentity(Options.ClaimsIssuer);

                if (Options.SaveTokensAsClaims)
                {
                    identity.AddClaim(new Claim("access_token", tokens.AccessToken,
                                                ClaimValueTypes.String, Options.ClaimsIssuer));

                    if (!string.IsNullOrEmpty(tokens.RefreshToken))
                    {
                        identity.AddClaim(new Claim("refresh_token", tokens.RefreshToken,
                                                    ClaimValueTypes.String, Options.ClaimsIssuer));
                    }

                    if (!string.IsNullOrEmpty(tokens.TokenType))
                    {
                        identity.AddClaim(new Claim("token_type", tokens.TokenType,
                                                    ClaimValueTypes.String, Options.ClaimsIssuer));
                    }

                    if (!string.IsNullOrEmpty(tokens.ExpiresIn))
                    {
                        identity.AddClaim(new Claim("expires_in", tokens.ExpiresIn,
                                                    ClaimValueTypes.String, Options.ClaimsIssuer));
                    }
                }
                
                return await CreateTicketAsync(identity, properties, tokens);
            }
            catch (Exception ex)
            {
                Logger.LogError("Authentication failed", ex);
                return new AuthenticationTicket(properties, Options.AuthenticationScheme);
            }
        }

        protected virtual async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            var tokenRequestParameters = new Dictionary<string, string>()
            {
                { "client_id", Options.ClientId },
                { "redirect_uri", redirectUri },
                { "client_secret", Options.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            response.EnsureSuccessStatusCode();
            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

            return new OAuthTokenResponse(payload);
        }

        protected virtual async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            var notification = new OAuthAuthenticatedContext(Context, Options, Backchannel, tokens)
            {
                Principal = new ClaimsPrincipal(identity),
                Properties = properties
            };
            
            await Options.Notifications.Authenticated(notification);

            if (notification.Principal?.Identity == null)
            {
                return null;
            }

            return new AuthenticationTicket(notification.Principal, notification.Properties, Options.AuthenticationScheme);
        }

        protected override Task<bool> HandleUnauthorizedAsync([NotNull] ChallengeContext context)
        {
            var properties = new AuthenticationProperties(context.Properties);
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }

            // OAuth2 10.12 CSRF
            GenerateCorrelationId(properties);

            var authorizationEndpoint = BuildChallengeUrl(properties, BuildRedirectUri(Options.CallbackPath));

            var redirectContext = new OAuthApplyRedirectContext(
                Context, Options,
                properties, authorizationEndpoint);
            Options.Notifications.ApplyRedirect(redirectContext);
            return Task.FromResult(true);
        }

        protected override Task HandleSignOutAsync(SignOutContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task HandleSignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task<bool> HandleForbiddenAsync(ChallengeContext context)
        {
            throw new NotSupportedException();
        }

        protected virtual string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var scope = FormatScope();

            var state = Options.StateDataFormat.Protect(properties);

            var queryBuilder = new QueryBuilder()
            {
                { "client_id", Options.ClientId },
                { "scope", scope },
                { "response_type", "code" },
                { "redirect_uri", redirectUri },
                { "state", state },
            };
            return Options.AuthorizationEndpoint + queryBuilder.ToString();
        }

        protected virtual string FormatScope()
        {
            // OAuth2 3.3 space separated
            return string.Join(" ", Options.Scope);
        }

        protected void GenerateCorrelationId([NotNull] AuthenticationProperties properties)
        {
            var correlationKey = Constants.CorrelationPrefix + Options.AuthenticationScheme;

            var nonceBytes = new byte[32];
            CryptoRandom.GetBytes(nonceBytes);
            var correlationId = TextEncodings.Base64Url.Encode(nonceBytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };

            properties.Items[correlationKey] = correlationId;

            Response.Cookies.Append(correlationKey, correlationId, cookieOptions);
        }

        protected bool ValidateCorrelationId([NotNull] AuthenticationProperties properties)
        {
            var correlationKey = Constants.CorrelationPrefix + Options.AuthenticationScheme;
            var correlationCookie = Request.Cookies[correlationKey];
            if (string.IsNullOrEmpty(correlationCookie))
            {
                Logger.LogWarning("{0} cookie not found.", correlationKey);
                return false;
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };
            Response.Cookies.Delete(correlationKey, cookieOptions);

            string correlationExtra;
            if (!properties.Items.TryGetValue(
                correlationKey,
                out correlationExtra))
            {
                Logger.LogWarning("{0} state property not found.", correlationKey);
                return false;
            }

            properties.Items.Remove(correlationKey);

            if (!string.Equals(correlationCookie, correlationExtra, StringComparison.Ordinal))
            {
                Logger.LogWarning("{0} correlation cookie and state property mismatch.", correlationKey);
                return false;
            }

            return true;
        }
    }
}