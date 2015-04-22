// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Http.Authentication;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    internal class MicrosoftAccountAuthenticationHandler : OAuthAuthenticationHandler<MicrosoftAccountAuthenticationOptions, IMicrosoftAccountAuthenticationNotifications>
    {
        public MicrosoftAccountAuthenticationHandler(HttpClient httpClient)
            : base(httpClient)
        {
        }

        protected override async Task<AuthenticationTicket> GetUserInformationAsync(AuthenticationProperties properties, TokenResponse tokens)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var graphResponse = await Backchannel.SendAsync(request, Context.RequestAborted);
            graphResponse.EnsureSuccessStatusCode();
            var accountString = await graphResponse.Content.ReadAsStringAsync();
            var accountInformation = JObject.Parse(accountString);

            var context = new MicrosoftAccountAuthenticatedContext(Context, Options, accountInformation, tokens);
            context.Properties = properties;
            var identity = new ClaimsIdentity(
                new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, context.Id, ClaimValueTypes.String, Options.AuthenticationScheme),
                        new Claim(ClaimTypes.Name, context.Name, ClaimValueTypes.String, Options.AuthenticationScheme),
                        new Claim("urn:microsoftaccount:id", context.Id, ClaimValueTypes.String, Options.AuthenticationScheme),
                        new Claim("urn:microsoftaccount:name", context.Name, ClaimValueTypes.String, Options.AuthenticationScheme)
                    },
                Options.AuthenticationScheme,
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);

            if (!string.IsNullOrWhiteSpace(context.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, context.Email, ClaimValueTypes.String, Options.AuthenticationScheme));
            }
            context.Principal = new ClaimsPrincipal(identity);

            await Options.Notifications.Authenticated(context);

            return new AuthenticationTicket(context.Principal, context.Properties, context.Options.AuthenticationScheme);
        }
    }
}
