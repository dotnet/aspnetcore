// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal static class AuthorizeHelper
    {
        public static async Task<bool> AuthorizeAsync(HttpContext context, IList<IAuthorizeData> policies)
        {
            if (policies.Count == 0)
            {
                return true;
            }

            var policyProvider = context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);

            var policyEvaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();

            // This will set context.User if required
            var authenticateResult = await policyEvaluator.AuthenticateAsync(authorizePolicy, context);

            var authorizeResult = await policyEvaluator.AuthorizeAsync(authorizePolicy, authenticateResult, context, resource: null);
            if (authorizeResult.Succeeded)
            {
                return true;
            }
            else if (authorizeResult.Challenged)
            {
                if (authorizePolicy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in authorizePolicy.AuthenticationSchemes)
                    {
                        await context.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }
                return false;
            }
            else if (authorizeResult.Forbidden)
            {
                if (authorizePolicy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in authorizePolicy.AuthenticationSchemes)
                    {
                        await context.ForbidAsync(scheme);
                    }
                }
                else
                {
                    await context.ForbidAsync();
                }
                return false;
            }
            return false;
        }
    }
}
