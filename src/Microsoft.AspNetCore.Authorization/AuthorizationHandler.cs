// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization
{
    public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
        where TRequirement : IAuthorizationRequirement
    {
        public void Handle(AuthorizationHandlerContext context)
        {
            foreach (var req in context.Requirements.OfType<TRequirement>())
            {
                Handle(context, req);
            }
        }

        public virtual async Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var req in context.Requirements.OfType<TRequirement>())
            {
                await HandleAsync(context, req);
            }
        }

        protected abstract void Handle(AuthorizationHandlerContext context, TRequirement requirement);

        protected virtual Task HandleAsync(AuthorizationHandlerContext context, TRequirement requirement)
        {
            Handle(context, requirement);
            return Task.FromResult(0);
        }
    }

    public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler
        where TRequirement : IAuthorizationRequirement
    {
        public virtual async Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context.Resource is TResource)
            {
                foreach (var req in context.Requirements.OfType<TRequirement>())
                {
                    await HandleAsync(context, req, (TResource)context.Resource);
                }
            }
        }

        protected virtual Task HandleAsync(AuthorizationHandlerContext context, TRequirement requirement, TResource resource)
        {
            Handle(context, requirement, resource);
            return Task.FromResult(0);
        }

        public virtual void Handle(AuthorizationHandlerContext context)
        {
            if (context.Resource is TResource)
            {
                foreach (var req in context.Requirements.OfType<TRequirement>())
                {
                    Handle(context, req, (TResource)context.Resource);
                }
            }
        }

        protected abstract void Handle(AuthorizationHandlerContext context, TRequirement requirement, TResource resource);
    }
}