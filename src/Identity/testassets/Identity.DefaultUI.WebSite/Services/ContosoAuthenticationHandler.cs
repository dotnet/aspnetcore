// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Identity.DefaultUI.WebSite
{
    public class ContosoAuthenticationHandler : AuthenticationHandler<ContosoAuthenticationOptions>
    {
        public ContosoAuthenticationHandler(
            IOptionsMonitor<ContosoAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var uri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Options.RemoteLoginPath}";
            uri = QueryHelpers.AddQueryString(uri, new Dictionary<string, string>()
            {
                ["State"] = JsonConvert.SerializeObject(properties.Items),
                [Options.ReturnUrlQueryParameter] = properties.RedirectUri
            });
            Response.Redirect(uri);

            return Task.CompletedTask;
        }
    }
}