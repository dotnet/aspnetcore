// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.OAuth
{
    public class OAuthAuthenticationHandler<TOptions, TNotifications> : AuthenticationHandler<TOptions>
        where TOptions : OAuthAuthenticationOptions<TNotifications>
        where TNotifications : IOAuthAuthenticationNotifications
    {
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
            AuthenticationTicket ticket = await AuthenticateAsync();
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
                Context.Response.SignIn(context.SignInScheme, context.Principal, context.Properties);
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

        protected override AuthenticationTicket AuthenticateCore()
        {
            return AuthenticateCoreAsync().GetAwaiter().GetResult();
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
                    Logger.LogVerbose("Remote server returned an error: " + Request.QueryString);
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
                if (!ValidateCorrelationId(properties))
                {
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                if (string.IsNullOrEmpty(code))
                {
                    // Null if the remote server returns an error.
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                string requestPrefix = Request.Scheme + "://" + Request.Host;
                string redirectUri = requestPrefix + RequestPathBase + Options.CallbackPath;

                var tokens = await ExchangeCodeAsync(code, redirectUri);

                if (string.IsNullOrWhiteSpace(tokens.AccessToken))
                {
                    Logger.LogWarning("Access token was not found");
                    return new AuthenticationTicket(properties, Options.AuthenticationScheme);
                }

                return await GetUserInformationAsync(properties, tokens);
            }
            catch (Exception ex)
            {
                Logger.LogError("Authentication failed", ex);
                return new AuthenticationTicket(properties, Options.AuthenticationScheme);
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
            return new AuthenticationTicket(context.Principal, context.Properties, Options.AuthenticationScheme);
        }

        protected override void ApplyResponseChallenge()
        {
            if (ShouldConvertChallengeToForbidden())
            {
                Response.StatusCode = 403;
                return;
            }

            if (Response.StatusCode != 401)
            {
                return;
            }

            // When Automatic should redirect on 401 even if there wasn't an explicit challenge.
            if (ChallengeContext == null && !Options.AutomaticAuthentication)
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
