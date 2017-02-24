// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public static class AuthorizeHelper
    {
        public static async Task<bool> AuthorizeAsync(HttpContext context, AuthorizationPolicy policy)
        {
            if (policy != null)
            {
                if (policy.AuthenticationSchemes != null && policy.AuthenticationSchemes.Count > 0)
                {
                    ClaimsPrincipal newPrincipal = null;
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        var result = await context.Authentication.AuthenticateAsync(scheme);
                        if (result != null)
                        {
                            newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result);
                        }
                    }

                    if (newPrincipal == null)
                    {
                        newPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
                    }

                    context.User = newPrincipal;
                }

                var authService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                if (await authService.AuthorizeAsync(context.User, context, policy))
                {
                    return true;
                }

                // Challenge
                if (policy.AuthenticationSchemes != null && policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.Authentication.ChallengeAsync(scheme, properties: null);
                    }
                }
                else
                {
                    await context.Authentication.ChallengeAsync(properties: null);
                }

                return false;
            }

            return true;
        }
    }
}
