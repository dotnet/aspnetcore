// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private RequestContext _requestContext;
        private AuthenticationScheme _scheme;

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            var identity = _requestContext.User?.Identity;
            if (identity != null && identity.IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_requestContext.User, properties: null, authenticationScheme: _scheme.Name)));
            }
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            _requestContext.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            _requestContext.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _requestContext = context.Features.Get<RequestContext>();

            if (_requestContext == null)
            {
                throw new InvalidOperationException("No RequestContext found.");
            }

            return Task.CompletedTask;
        }
    }
}