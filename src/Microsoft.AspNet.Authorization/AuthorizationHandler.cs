// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authorization
{
    public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
        where TRequirement : IAuthorizationRequirement
    {
        public void Handle(AuthorizationContext context)
        {
            foreach (var req in context.Requirements.OfType<TRequirement>())
            {
                Handle(context, req);
            }
        }

        public virtual async Task HandleAsync(AuthorizationContext context)
        {
            foreach (var req in context.Requirements.OfType<TRequirement>())
            {
                await HandleAsync(context, req);
            }
        }

        protected abstract void Handle(AuthorizationContext context, TRequirement requirement);

        protected virtual Task HandleAsync(AuthorizationContext context, TRequirement requirement)
        {
            Handle(context, requirement);
            return Task.FromResult(0);
        }
    }

    public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler
        where TResource : class
        where TRequirement : IAuthorizationRequirement
    {
        public virtual async Task HandleAsync(AuthorizationContext context)
        {
            if (context.Resource is TResource)
            {
                foreach (var req in context.Requirements.OfType<TRequirement>())
                {
                    await HandleAsync(context, req, (TResource)context.Resource);
                }
            }
        }

        protected virtual Task HandleAsync(AuthorizationContext context, TRequirement requirement, TResource resource)
        {
            Handle(context, requirement, resource);
            return Task.FromResult(0);
        }

        public virtual void Handle(AuthorizationContext context)
        {
            var resource = context.Resource as TResource;
            // REVIEW: should we allow null resources?
            if (resource != null)
            {
                foreach (var req in context.Requirements.OfType<TRequirement>())
                {
                    Handle(context, req, resource);
                }
            }
        }

        protected abstract void Handle(AuthorizationContext context, TRequirement requirement, TResource resource);
    }
}