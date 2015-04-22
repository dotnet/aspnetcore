// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.WebUtilities;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Google
{
    internal class GoogleAuthenticationHandler : OAuthAuthenticationHandler<GoogleAuthenticationOptions, IGoogleAuthenticationNotifications>
    {
        public GoogleAuthenticationHandler(HttpClient httpClient)
            : base(httpClient)
        {
        }

        protected override async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, TokenResponse tokens)
        {
            // Get the Google user
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var graphResponse = await Backchannel.SendAsync(request, Context.RequestAborted);
            graphResponse.EnsureSuccessStatusCode();
            var text = await graphResponse.Content.ReadAsStringAsync();
            var user = JObject.Parse(text);

            var context = new GoogleAuthenticatedContext(Context, Options, user, tokens);
            var identity = new ClaimsIdentity(
                Options.AuthenticationScheme,
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);

            if (!string.IsNullOrEmpty(context.Id))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.Id,
                    ClaimValueTypes.String, Options.AuthenticationScheme));
            }
            if (!string.IsNullOrEmpty(context.GivenName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, context.GivenName,
                    ClaimValueTypes.String, Options.AuthenticationScheme));
            }
            if (!string.IsNullOrEmpty(context.FamilyName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, context.FamilyName,
                    ClaimValueTypes.String, Options.AuthenticationScheme));
            }
            if (!string.IsNullOrEmpty(context.Name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, context.Name, ClaimValueTypes.String,
                    Options.AuthenticationScheme));
            }
            if (!string.IsNullOrEmpty(context.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, ClaimValueTypes.String,
                    Options.AuthenticationScheme));
            }
            if (!string.IsNullOrEmpty(context.Profile))
            {
                identity.AddClaim(new Claim("urn:google:profile", context.Profile, ClaimValueTypes.String,
                    Options.AuthenticationScheme));
            }
            context.Properties = properties;
            context.Principal = new ClaimsPrincipal(identity);

            await Options.Notifications.Authenticated(context);

            return new AuthenticationTicket(context.Principal, context.Properties, context.Options.AuthenticationScheme);
        }

        // TODO: Abstract this properties override pattern into the base class?
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var scope = FormatScope();

            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            queryStrings.Add("response_type", "code");
            queryStrings.Add("client_id", Options.ClientId);
            queryStrings.Add("redirect_uri", redirectUri);

            AddQueryString(queryStrings, properties, "scope", scope);

            AddQueryString(queryStrings, properties, "access_type", Options.AccessType);
            AddQueryString(queryStrings, properties, "approval_prompt");
            AddQueryString(queryStrings, properties, "login_hint");

            var state = Options.StateDataFormat.Protect(properties);
            queryStrings.Add("state", state);

            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
            return authorizationEndpoint;
        }

        private static void AddQueryString(IDictionary<string, string> queryStrings, AuthenticationProperties properties,
            string name, string defaultValue = null)
        {
            string value;
            if (!properties.Dictionary.TryGetValue(name, out value))
            {
                value = defaultValue;
            }
            else
            {
                // Remove the parameter from AuthenticationProperties so it won't be serialized to state parameter
                properties.Dictionary.Remove(name);
            }

            if (value == null)
            {
                return;
            }

            queryStrings[name] = value;
        }
    }
}
