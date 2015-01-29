// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Security
{
    public class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IList<IAuthorizationHandler> _handlers;
        private readonly AuthorizationOptions _options;

        public DefaultAuthorizationService(IOptions<AuthorizationOptions> options, IEnumerable<IAuthorizationHandler> handlers)
        {
            _handlers = handlers.ToArray();
            _options = options.Options;
        }

        public Task<bool> AuthorizeAsync([NotNull] string policyName, HttpContext context, object resource = null)
        {
            var policy = _options.GetPolicy(policyName);
            if (policy == null)
            {
                return Task.FromResult(false);
            }
            return AuthorizeAsync(policy, context, resource);
        }

        public async Task<bool> AuthorizeAsync([NotNull] AuthorizationPolicy policy, [NotNull] HttpContext context, object resource = null)
        {
            var user = context.User;
            try
            {
                // Generate the user identities if policy specified the AuthTypes
                if (policy.ActiveAuthenticationTypes != null && policy.ActiveAuthenticationTypes.Any() )
                {
                    var principal = new ClaimsPrincipal();

                    var results = await context.AuthenticateAsync(policy.ActiveAuthenticationTypes);
                    // REVIEW: re requesting the identities fails for MVC currently, so we only request if not found
                    foreach (var result in results)
                    {
                        principal.AddIdentity(result.Identity);
                    }
                    context.User = principal;
                }

                var authContext = new AuthorizationContext(policy, context, resource);

                foreach (var handler in _handlers)
                {
                    await handler.HandleAsync(authContext);
                }
                return authContext.HasSucceeded;
            }
            finally
            {
                context.User = user;
            }
        }
    }
}