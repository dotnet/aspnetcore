// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private WindowsPrincipal _user;
        private HttpContext _context;
        private AuthenticationScheme _scheme;

        public Task<AuthenticateResult> AuthenticateAsync() 
        {
            if (_user != null)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_user, _scheme.Name)));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            // We would normally set the www-authenticate header here, but IIS does that for us.
            _context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            _context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _context = context;
            _user = context.Features.Get<WindowsPrincipal>(); // See IISMiddleware
            return Task.CompletedTask;
        }
    }
}
