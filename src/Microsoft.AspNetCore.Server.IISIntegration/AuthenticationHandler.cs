// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        internal AuthenticationHandler(HttpContext httpContext, IISOptions options, ClaimsPrincipal user)
        {
            HttpContext = httpContext;
            User = user;
            Options = options;
        }

        internal HttpContext HttpContext { get; }

        internal IISOptions Options { get; }

        internal ClaimsPrincipal User { get; }

        internal IAuthenticationHandler PriorHandler { get; set; }

        public Task AuthenticateAsync(AuthenticateContext context)
        {
            if (ShouldHandleScheme(context.AuthenticationScheme))
            {
                if (User != null)
                {
                    context.Authenticated(User, properties: null, description: null);
                }
                else
                {
                    context.NotAuthenticated();
                }
            }

            if (PriorHandler != null)
            {
                return PriorHandler.AuthenticateAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        public Task ChallengeAsync(ChallengeContext context)
        {
            // Some other provider may have already accepted this challenge. Having multiple providers with
            // AutomaticChallenge = true is considered invalid, but changing the default would breaking
            // normal Windows auth users.
            if (!context.Accepted && ShouldHandleScheme(context.AuthenticationScheme))
            {
                switch (context.Behavior)
                {
                    case ChallengeBehavior.Automatic:
                        // If there is a principal already, invoke the forbidden code path
                        if (User == null)
                        {
                            goto case ChallengeBehavior.Unauthorized;
                        }
                        else
                        {
                            goto case ChallengeBehavior.Forbidden;
                        }
                    case ChallengeBehavior.Unauthorized:
                        HttpContext.Response.StatusCode = 401;
                        // We would normally set the www-authenticate header here, but IIS does that for us.
                        break;
                    case ChallengeBehavior.Forbidden:
                        HttpContext.Response.StatusCode = 403;
                        break;
                }
                context.Accept();
            }

            if (PriorHandler != null)
            {
                return PriorHandler.ChallengeAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        public void GetDescriptions(DescribeSchemesContext context)
        {
            foreach (var description in Options.AuthenticationDescriptions)
            {
                context.Accept(description.Items);
            }

            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(context);
            }
        }

        public Task SignInAsync(SignInContext context)
        {
            // Not supported, fall through
            if (PriorHandler != null)
            {
                return PriorHandler.SignInAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        public Task SignOutAsync(SignOutContext context)
        {
            // Not supported, fall through
            if (PriorHandler != null)
            {
                return PriorHandler.SignOutAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        private bool ShouldHandleScheme(string authenticationScheme)
        {
            if (Options.AutomaticAuthentication && string.Equals(AuthenticationManager.AutomaticScheme, authenticationScheme, StringComparison.Ordinal))
            {
                return true;
            }

            return Options.AuthenticationDescriptions.Any(description => string.Equals(description.AuthenticationScheme, authenticationScheme, StringComparison.Ordinal));
        }
    }
}
