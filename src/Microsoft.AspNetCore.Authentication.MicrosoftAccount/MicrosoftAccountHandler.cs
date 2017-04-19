// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{
    internal class MicrosoftAccountHandler : OAuthHandler<MicrosoftAccountOptions>
    {
        public MicrosoftAccountHandler(IOptions<AuthenticationOptions> sharedOptions, IOptionsSnapshot<MicrosoftAccountOptions> options, ILoggerFactory logger, UrlEncoder encoder, IDataProtectionProvider dataProtection, ISystemClock clock)
            : base(sharedOptions, options, logger, encoder, dataProtection, clock)
        { }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var response = await Backchannel.SendAsync(request, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrived Microsoft user information ({response.StatusCode}) Please check if the authentication information is correct and the corresponding Microsoft Account API is enabled.");
            }

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Scheme.Name);
            var context = new OAuthCreatingTicketContext(ticket, Context, Scheme, Options, Backchannel, tokens, payload);
            context.RunClaimActions();

            await Events.CreatingTicket(context);
            return context.Ticket;
        }
    }
}
