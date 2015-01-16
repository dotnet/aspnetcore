// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Security;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class AuthorizeAttribute : AuthorizationFilterAttribute
    {
        private string _roles;
        private string[] _rolesSplit;

        public AuthorizeAttribute() { }

        public AuthorizeAttribute(string policy)
        {
            Policy = policy;
        }

        public string Policy { get; set; }

        public string Roles
        {
            get { return _roles; }
            set
            {
                _roles = value;
                if (string.IsNullOrWhiteSpace(_roles))
                {
                    _rolesSplit = null;
                }
                else
                {
                    _rolesSplit = _roles.Split(',');
                }
            }
        }

        public override async Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
            var httpContext = context.HttpContext;

            // Allow Anonymous skips all authorization
            if (HasAllowAnonymous(context))
            {
                return;
            }

            var authService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            // Build a policy for the requested roles if specified
            if (_rolesSplit != null)
            {
                var rolesPolicy = new AuthorizationPolicyBuilder();
                rolesPolicy.RequiresRole(_rolesSplit);
                if (!await authService.AuthorizeAsync(rolesPolicy.Build(), httpContext, context))
                {
                    Fail(context);
                    return;
                }
            }

            var authorized = (Policy == null)
                // [Authorize] with no policy just requires any authenticated user
                ? await authService.AuthorizeAsync(BuildAnyAuthorizedUserPolicy(), httpContext, context)
                : await authService.AuthorizeAsync(Policy, httpContext, context);
            if (!authorized)
            {
                Fail(context);
            }
        }

        private static AuthorizationPolicy BuildAnyAuthorizedUserPolicy()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        }

        public sealed override void OnAuthorization([NotNull] AuthorizationContext context)
        {
            // The async filter will be called by the filter pipeline.
            throw new NotImplementedException(Resources.AuthorizeAttribute_OnAuthorizationNotImplemented);
        }
    }
}
