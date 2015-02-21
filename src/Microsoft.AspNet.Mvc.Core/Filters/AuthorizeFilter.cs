// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Security;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An implementation of <see cref="IAsyncAuthorizationFilter"/>
    /// </summary>
    public class AuthorizeFilter : IAsyncAuthorizationFilter
    {
        /// <summary>
        /// Authorize filter for a specific policy.
        /// </summary>
        /// <param name="policy"></param>
        public AuthorizeFilter([NotNull] AuthorizationPolicy policy)
        {
            Policy = policy;
        }

        /// <summary>
        /// Authorization policy to be used.
        /// </summary>
        public AuthorizationPolicy Policy { get; private set; }

        /// <inheritdoc />
        public virtual async Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
            // Build a ClaimsPrincipal with the Policy's required authentication types
            if (Policy.ActiveAuthenticationTypes != null && Policy.ActiveAuthenticationTypes.Any())
            {
                var results = await context.HttpContext.AuthenticateAsync(Policy.ActiveAuthenticationTypes);
                if (results != null)
                {
                    context.HttpContext.User = new ClaimsPrincipal(results.Where(r => r.Identity != null).Select(r => r.Identity));
                }
            }

            // Allow Anonymous skips all authorization
            if (context.Filters.Any(item => item is IAllowAnonymous))
            {
                return;
            }

            var httpContext = context.HttpContext;
            var authService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            // Note: Default Anonymous User is new ClaimsPrincipal(new ClaimsIdentity())
            if (httpContext.User == null ||
                !httpContext.User.Identities.Any(i => i.IsAuthenticated) ||
                !await authService.AuthorizeAsync(httpContext.User, context, Policy))
            {
                context.Result = new ChallengeResult(Policy.ActiveAuthenticationTypes.ToArray());
            }
        }
    }
}
