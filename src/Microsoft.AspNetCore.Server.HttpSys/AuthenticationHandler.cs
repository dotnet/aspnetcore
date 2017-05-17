// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

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
            return Task.FromResult(AuthenticateResult.None());
        }

        public Task ChallengeAsync(ChallengeContext context)
        {
            switch (context.Behavior)
            {
                case ChallengeBehavior.Forbidden:
                    _requestContext.Response.StatusCode = 403;
                    break;
                case ChallengeBehavior.Unauthorized:
                    _requestContext.Response.StatusCode = 401;
                    break;
                case ChallengeBehavior.Automatic:
                    var identity = (ClaimsIdentity)_requestContext.User?.Identity;
                    if (identity != null && identity.IsAuthenticated)
                    {
                        _requestContext.Response.StatusCode = 403;
                    }
                    else
                    {
                        _requestContext.Response.StatusCode = 401;
                    }
                    break;
                default:
                    throw new NotSupportedException(context.Behavior.ToString());
            }

            return TaskCache.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _requestContext = context.Features.Get<RequestContext>();

            if (_requestContext == null)
            {
                throw new InvalidOperationException("No RequestContext found.");
            }

            return TaskCache.CompletedTask;
        }

        public Task SignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }

        public Task SignOutAsync(SignOutContext context)
        {
            return TaskCache.CompletedTask;
        }
    }
}