// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Security.MicrosoftAccount
{
    internal class MicrosoftAccountAuthenticationHandler : AuthenticationHandler<MicrosoftAccountAuthenticationOptions>
    {
        private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
        private const string GraphApiEndpoint = "https://apis.live.net/v5.0/me";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public MicrosoftAccountAuthenticationHandler(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync();
            }
            return false;
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
                string code = null;
                string state = null;

                IReadableStringCollection query = Request.Query;
                IList<string> values = query.GetValues("code");
                if (values != null && values.Count == 1)
                {
                    code = values[0];
                }
                values = query.GetValues("state");
                if (values != null && values.Count == 1)
                {
                    state = values[0];
                }

                properties = Options.StateDataFormat.Unprotect(state);
                if (properties == null)
                {
                    return null;
                }

                // OAuth2 10.12 CSRF
                if (!ValidateCorrelationId(properties, _logger))
                {
                    return new AuthenticationTicket(null, properties);
                }

                var tokenRequestParameters = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("client_id", Options.ClientId),
                    new KeyValuePair<string, string>("redirect_uri", GenerateRedirectUri()),
                    new KeyValuePair<string, string>("client_secret", Options.ClientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                };

                var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

                HttpResponseMessage response = await _httpClient.PostAsync(TokenEndpoint, requestContent, Context.RequestAborted);
                response.EnsureSuccessStatusCode();
                string oauthTokenResponse = await response.Content.ReadAsStringAsync();

                JObject oauth2Token = JObject.Parse(oauthTokenResponse);
                var accessToken = oauth2Token["access_token"].Value<string>();

                // Refresh token is only available when wl.offline_access is request.
                // Otherwise, it is null.
                var refreshToken = oauth2Token.Value<string>("refresh_token");
                var expire = oauth2Token.Value<string>("expires_in");

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.WriteWarning("Access token was not found");
                    return new AuthenticationTicket(null, properties);
                }

                HttpResponseMessage graphResponse = await _httpClient.GetAsync(
                    GraphApiEndpoint + "?access_token=" + Uri.EscapeDataString(accessToken), Context.RequestAborted);
                graphResponse.EnsureSuccessStatusCode();
                string accountString = await graphResponse.Content.ReadAsStringAsync();
                JObject accountInformation = JObject.Parse(accountString);

                var context = new MicrosoftAccountAuthenticatedContext(Context, accountInformation, accessToken,
                    refreshToken, expire);
                context.Identity = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, context.Id, "http://www.w3.org/2001/XMLSchema#string", Options.AuthenticationType),
                        new Claim(ClaimTypes.Name, context.Name, "http://www.w3.org/2001/XMLSchema#string", Options.AuthenticationType),
                        new Claim("urn:microsoftaccount:id", context.Id, "http://www.w3.org/2001/XMLSchema#string", Options.AuthenticationType),
                        new Claim("urn:microsoftaccount:name", context.Name, "http://www.w3.org/2001/XMLSchema#string", Options.AuthenticationType)
                    },
                    Options.AuthenticationType,
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                if (!string.IsNullOrWhiteSpace(context.Email))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, "http://www.w3.org/2001/XMLSchema#string", Options.AuthenticationType));
                }

                await Options.Notifications.Authenticated(context);

                context.Properties = properties;

                return new AuthenticationTicket(context.Identity, context.Properties);
            }
            catch (Exception ex)
            {
                _logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
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

            // OAuth2 3.3 space separated                
            string scope = string.Join(" ", Options.Scope);
            // LiveID requires a scope string, so if the user didn't set one we go for the least possible.
            if (string.IsNullOrWhiteSpace(scope))
            {
                scope = "wl.basic";
            }

            string state = Options.StateDataFormat.Protect(properties);

            string authorizationEndpoint =
                "https://login.live.com/oauth20_authorize.srf" +
                    "?client_id=" + Uri.EscapeDataString(Options.ClientId) +
                    "&scope=" + Uri.EscapeDataString(scope) + 
                    "&response_type=code" +
                    "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                    "&state=" + Uri.EscapeDataString(state);

            var redirectContext = new MicrosoftAccountApplyRedirectContext(
                Context, Options,
                properties, authorizationEndpoint);
            Options.Notifications.ApplyRedirect(redirectContext);
        }

        public async Task<bool> InvokeReturnPathAsync()
        {
            AuthenticationTicket model = await AuthenticateAsync();
            if (model == null)
            {
                _logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new MicrosoftAccountReturnEndpointContext(Context, model);
            context.SignInAsAuthenticationType = Options.SignInAsAuthenticationType;
            context.RedirectUri = model.Properties.RedirectUri;
            model.Properties.RedirectUri = null;

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

        private string GenerateRedirectUri()
        {
            string requestPrefix = Request.Scheme + "://" + Request.Host;

            return requestPrefix + RequestPathBase + Options.CallbackPath;
        }

        protected override void ApplyResponseGrant()
        {
            // N/A - No SignIn or SignOut support.
        }
    }
}
