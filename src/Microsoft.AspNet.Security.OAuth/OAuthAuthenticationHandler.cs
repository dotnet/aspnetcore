// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Security.OAuth
{
    public class OAuthAuthenticationHandler<TOptions, TNotifications> : AuthenticationHandler<TOptions>
        where TOptions : OAuthAuthenticationOptions<TNotifications>
        where TNotifications : IOAuthAuthenticationNotifications
    {
        public OAuthAuthenticationHandler(HttpClient backchannel, ILogger logger)
        {
            Backchannel = backchannel;
            Logger = logger;
        }

        protected HttpClient Backchannel { get; private set; }

        protected ILogger Logger { get; private set; }

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
            AuthenticationTicket ticket = await AuthenticateAsync();
            if (ticket == null)
            {
                Logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new OAuthReturnEndpointContext(Context, ticket)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = ticket.Properties.RedirectUri,
            };
            ticket.Properties.RedirectUri = null;

            await Options.Notifications.ReturnEndpoint(context);

            if (context.SignInAsAuthenticationType != null && context.Identity != null)
            {
                ClaimsIdentity signInIdentity = context.Identity;
                if (!string.Equals(signInIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.Ordinal))
                {
                    signInIdentity = new ClaimsIdentity(signInIdentity.Claims, context.SignInAsAuthenticationType, signInIdentity.NameClaimType, signInIdentity.RoleClaimType);
                }
                Context.Response.SignIn(context.Properties, signInIdentity);
            }

            if (!context.IsRequestCompleted && context.RedirectUri != null)
            {
                if (context.Identity == null)
                {
                    // add a redirect hint that sign-in failed in some way
                    context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "error", "access_denied");
                }
                Response.Redirect(context.RedirectUri);
                context.RequestCompleted();
            }

            return context.IsRequestCompleted;
        }

        protected override AuthenticationTicket AuthenticateCore()
        {
            return AuthenticateCoreAsync().Result;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            AuthenticationProperties properties = null;
            try
            {
                IReadableStringCollection query = Request.Query;

                // TODO: Is this a standard error returned by servers?
                var value = query.Get("error");
                if (!string.IsNullOrEmpty(value))
                {
                    Logger.WriteVerbose("Remote server returned an error: " + Request.QueryString);
                    // TODO: Fail request rather than passing through?
                    return null;
                }

                string code = query.Get("code");
                string state = query.Get("state");

                properties = Options.StateDataFormat.Unprotect(state);
                if (properties == null)
                {
                    return null;
                }

                // OAuth2 10.12 CSRF
                if (!ValidateCorrelationId(properties, Logger))
                {
                    return new AuthenticationTicket(null, properties);
                }

                if (string.IsNullOrEmpty(code))
                {
                    // Null if the remote server returns an error.
                    return new AuthenticationTicket(null, properties);
                }

                string requestPrefix = Request.Scheme + "://" + Request.Host;
                string redirectUri = requestPrefix + RequestPathBase + Options.CallbackPath;

                var tokens = await ExchangeCodeAsync(code, redirectUri);

                if (string.IsNullOrWhiteSpace(tokens.AccessToken))
                {
                    Logger.WriteWarning("Access token was not found");
                    return new AuthenticationTicket(null, properties);
                }

                return await GetUserInformationAsync(properties, tokens);
            }
            catch (Exception ex)
            {
                Logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
        }

        protected virtual async Task<TokenResponse> ExchangeCodeAsync(string code, string redirectUri)
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
            HttpResponseMessage response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            response.EnsureSuccessStatusCode();
            string oauthTokenResponse = await response.Content.ReadAsStringAsync();

            JObject oauth2Token = JObject.Parse(oauthTokenResponse);
            return new TokenResponse(oauth2Token);
        }

        protected virtual async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, TokenResponse tokens)
        {
            var context = new OAuthGetUserInformationContext(Context, Options, Backchannel, tokens)
            {
                Properties = properties,
            };
            await Options.Notifications.GetUserInformationAsync(context);
            return new AuthenticationTicket(context.Identity, context.Properties);
        }

        protected override void ApplyResponseChallenge()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            // Active middleware should redirect on 401 even if there wasn't an explicit challenge.
            if (ChallengeContext == null && Options.AuthenticationMode == AuthenticationMode.Passive)
            {
                return;
            }

            string baseUri = Request.Scheme + "://" + Request.Host + Request.PathBase;

            string currentUri = baseUri + Request.Path + Request.QueryString;

            string redirectUri = baseUri + Options.CallbackPath;

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
                properties.RedirectUri = currentUri;
            }

            // OAuth2 10.12 CSRF
            GenerateCorrelationId(properties);

            string authorizationEndpoint = BuildChallengeUrl(properties, redirectUri);

            var redirectContext = new OAuthApplyRedirectContext(
                Context, Options,
                properties, authorizationEndpoint);
            Options.Notifications.ApplyRedirect(redirectContext);
        }

        protected virtual string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            string scope = FormatScope();

            string state = Options.StateDataFormat.Protect(properties);

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

        protected override void ApplyResponseGrant()
        {
            // N/A - No SignIn or SignOut support.
        }
    }
}
