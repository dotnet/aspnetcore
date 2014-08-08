// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Security.Facebook
{
    internal class FacebookAuthenticationHandler : AuthenticationHandler<FacebookAuthenticationOptions>
    {
        private const string XmlSchemaString = "http://www.w3.org/2001/XMLSchema#string";
        private const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";
        private const string GraphApiEndpoint = "https://graph.facebook.com/me";
        private const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public FacebookAuthenticationHandler(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
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

                IList<string> values = query.GetValues("error");
                if (values != null && values.Count >= 1)
                {
                    _logger.WriteVerbose("Remote server returned an error: " + Request.QueryString);
                }

                values = query.GetValues("code");
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

                if (code == null)
                {
                    // Null if the remote server returns an error.
                    return new AuthenticationTicket(null, properties);
                }

                string requestPrefix = Request.Scheme + "://" + Request.Host;
                string redirectUri = requestPrefix + Request.PathBase + Options.CallbackPath;

                string tokenRequest = "grant_type=authorization_code" +
                    "&code=" + Uri.EscapeDataString(code) +
                    "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                    "&client_id=" + Uri.EscapeDataString(Options.AppId) +
                    "&client_secret=" + Uri.EscapeDataString(Options.AppSecret);

                var tokenResponse = await _httpClient.GetAsync(TokenEndpoint + "?" + tokenRequest, Context.RequestAborted);
                tokenResponse.EnsureSuccessStatusCode();
                string text = await tokenResponse.Content.ReadAsStringAsync();
                IFormCollection form = FormHelpers.ParseForm(text);

                string accessToken = form["access_token"];
                string expires = form["expires"];
                string graphAddress = GraphApiEndpoint + "?access_token=" + Uri.EscapeDataString(accessToken);
                if (Options.SendAppSecretProof)
                {
                    graphAddress += "&appsecret_proof=" + GenerateAppSecretProof(accessToken);
                }

                var graphResponse = await _httpClient.GetAsync(graphAddress, Context.RequestAborted);
                graphResponse.EnsureSuccessStatusCode();
                text = await graphResponse.Content.ReadAsStringAsync();
                JObject user = JObject.Parse(text);

                var context = new FacebookAuthenticatedContext(Context, user, accessToken, expires);
                context.Identity = new ClaimsIdentity(
                    Options.AuthenticationType,
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                if (!string.IsNullOrEmpty(context.Id))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.Id, XmlSchemaString, Options.AuthenticationType));
                }
                if (!string.IsNullOrEmpty(context.UserName))
                {
                    context.Identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, context.UserName, XmlSchemaString, Options.AuthenticationType));
                }
                if (!string.IsNullOrEmpty(context.Email))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, XmlSchemaString, Options.AuthenticationType));
                }
                if (!string.IsNullOrEmpty(context.Name))
                {
                    context.Identity.AddClaim(new Claim("urn:facebook:name", context.Name, XmlSchemaString, Options.AuthenticationType));

                    // Many Facebook accounts do not set the UserName field.  Fall back to the Name field instead.
                    if (string.IsNullOrEmpty(context.UserName))
                    {
                        context.Identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, context.Name, XmlSchemaString, Options.AuthenticationType));
                    }
                }
                if (!string.IsNullOrEmpty(context.Link))
                {
                    context.Identity.AddClaim(new Claim("urn:facebook:link", context.Link, XmlSchemaString, Options.AuthenticationType));
                }
                context.Properties = properties;

                await Options.Notifications.Authenticated(context);

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

            string baseUri = 
                Request.Scheme + 
                "://" + 
                Request.Host +
                Request.PathBase;

            string currentUri =
                baseUri + 
                Request.Path +
                Request.QueryString;

            string redirectUri =
                baseUri + 
                Options.CallbackPath;

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

            // comma separated
            string scope = string.Join(",", Options.Scope);

            string state = Options.StateDataFormat.Protect(properties);

            string authorizationEndpoint =
                    AuthorizationEndpoint +
                    "?response_type=code" +
                    "&client_id=" + Uri.EscapeDataString(Options.AppId) +
                    "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                    "&scope=" + Uri.EscapeDataString(scope) +
                    "&state=" + Uri.EscapeDataString(state);

            var redirectContext = new FacebookApplyRedirectContext(Context, Options, properties, authorizationEndpoint);
            Options.Notifications.ApplyRedirect(redirectContext);
        }

        protected override void ApplyResponseGrant()
        {
            // N/A
        }

        public override async Task<bool> InvokeAsync()
        {
            return await InvokeReplyPathAsync();
        }

        private async Task<bool> InvokeReplyPathAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                // TODO: error responses

                AuthenticationTicket ticket = await AuthenticateAsync();
                if (ticket == null)
                {
                    _logger.WriteWarning("Invalid return state, unable to redirect.");
                    Response.StatusCode = 500;
                    return true;
                }

                var context = new FacebookReturnEndpointContext(Context, ticket);
                context.SignInAsAuthenticationType = Options.SignInAsAuthenticationType;
                context.RedirectUri = ticket.Properties.RedirectUri;

                await Options.Notifications.ReturnEndpoint(context);

                if (context.SignInAsAuthenticationType != null &&
                    context.Identity != null)
                {
                    ClaimsIdentity grantIdentity = context.Identity;
                    if (!string.Equals(grantIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.Ordinal))
                    {
                        grantIdentity = new ClaimsIdentity(grantIdentity.Claims, context.SignInAsAuthenticationType, grantIdentity.NameClaimType, grantIdentity.RoleClaimType);
                    }
                    Context.Response.SignIn(context.Properties, grantIdentity);
                }

                if (!context.IsRequestCompleted && context.RedirectUri != null)
                {
                    string redirectUri = context.RedirectUri;
                    if (context.Identity == null)
                    {
                        // add a redirect hint that sign-in failed in some way
                        redirectUri = QueryHelpers.AddQueryString(redirectUri, "error", "access_denied");
                    }
                    Response.Redirect(redirectUri);
                    context.RequestCompleted();
                }

                return context.IsRequestCompleted;
            }
            return false;
        }

        private string GenerateAppSecretProof(string accessToken)
        {
            using (HMACSHA256 algorithm = new HMACSHA256(Encoding.ASCII.GetBytes(Options.AppSecret)))
            {
                byte[] hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }
        }
    }
}
