// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Facebook
{
    internal class FacebookAuthenticationHandler : OAuthAuthenticationHandler<FacebookAuthenticationOptions, IFacebookAuthenticationNotifications>
    {
        public FacebookAuthenticationHandler(HttpClient httpClient)
            : base(httpClient)
        {
        }

        protected override async Task<TokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            var queryBuilder = new QueryBuilder()
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", Options.AppId },
                { "client_secret", Options.AppSecret },
            };

            var tokenResponse = await Backchannel.GetAsync(Options.TokenEndpoint + queryBuilder.ToString(), Context.RequestAborted);
            tokenResponse.EnsureSuccessStatusCode();
            var oauthTokenResponse = await tokenResponse.Content.ReadAsStringAsync();

            var form = new FormCollection(FormReader.ReadForm(oauthTokenResponse));
            var response = new JObject();
            foreach (string key in form.Keys)
            {
                response.Add(string.Equals(key, "expires", StringComparison.OrdinalIgnoreCase) ? "expires_in" : key, form[key]);
            }
            // The refresh token is not available.
            return new TokenResponse(response);
        }

        protected override async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, TokenResponse tokens)
        {
            var graphAddress = Options.UserInformationEndpoint + "?access_token=" + UrlEncoder.UrlEncode(tokens.AccessToken);
            if (Options.SendAppSecretProof)
            {
                graphAddress += "&appsecret_proof=" + GenerateAppSecretProof(tokens.AccessToken);
            }

            var graphResponse = await Backchannel.GetAsync(graphAddress, Context.RequestAborted);
            graphResponse.EnsureSuccessStatusCode();
            var text = await graphResponse.Content.ReadAsStringAsync();
            var user = JObject.Parse(text);

            var context = new FacebookAuthenticatedContext(Context, Options, user, tokens);
            var identity = new ClaimsIdentity(
                Options.ClaimsIssuer,
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            if (!string.IsNullOrEmpty(context.Id))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.Id, ClaimValueTypes.String, Options.ClaimsIssuer));
            }
            if (!string.IsNullOrEmpty(context.UserName))
            {
                identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, context.UserName, ClaimValueTypes.String, Options.ClaimsIssuer));
            }
            if (!string.IsNullOrEmpty(context.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, ClaimValueTypes.String, Options.ClaimsIssuer));
            }
            if (!string.IsNullOrEmpty(context.Name))
            {
                identity.AddClaim(new Claim("urn:facebook:name", context.Name, ClaimValueTypes.String, Options.ClaimsIssuer));

                // Many Facebook accounts do not set the UserName field.  Fall back to the Name field instead.
                if (string.IsNullOrEmpty(context.UserName))
                {
                    identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, context.Name, ClaimValueTypes.String, Options.ClaimsIssuer));
                }
            }
            if (!string.IsNullOrEmpty(context.Link))
            {
                identity.AddClaim(new Claim("urn:facebook:link", context.Link, ClaimValueTypes.String, Options.ClaimsIssuer));
            }
            context.Properties = properties;
            context.Principal = new ClaimsPrincipal(identity);

            await Options.Notifications.Authenticated(context);

            return new AuthenticationTicket(context.Principal, context.Properties, context.Options.AuthenticationScheme);
        }

        private string GenerateAppSecretProof(string accessToken)
        {
            using (var algorithm = new HMACSHA256(Encoding.ASCII.GetBytes(Options.AppSecret)))
            {
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
                var builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }
        }

        protected override string FormatScope()
        {
            // Facebook deviates from the OAuth spec here. They require comma separated instead of space separated.
            // https://developers.facebook.com/docs/reference/dialogs/oauth
            // http://tools.ietf.org/html/rfc6749#section-3.3
            return string.Join(",", Options.Scope);
        }
    }
}