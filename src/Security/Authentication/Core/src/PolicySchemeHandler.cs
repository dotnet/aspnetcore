// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// PolicySchemes are used to redirect authentication methods to another scheme.
    /// </summary>
    public class PolicySchemeHandler : SignInAuthenticationHandler<PolicySchemeOptions>
    {
        public PolicySchemeHandler(IOptionsMonitor<PolicySchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        { }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
            => throw new NotImplementedException();

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
            => throw new NotImplementedException();

        protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            => throw new NotImplementedException();

        protected override Task HandleSignOutAsync(AuthenticationProperties properties)
            => throw new NotImplementedException();

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => throw new NotImplementedException();
    }
}