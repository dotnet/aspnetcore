// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
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

        public virtual Task HandleAsync(AuthorizationContext context)
        {
            Handle(context);
            return Task.FromResult(0);
        }

        // REVIEW: do we need an async hook too?
        public abstract void Handle(AuthorizationContext context, TRequirement requirement);
    }

    public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler
        where TResource : class
        where TRequirement : IAuthorizationRequirement
    {
        public virtual async Task HandleAsync(AuthorizationContext context)
        {
            var resource = context.Resource as TResource;
            // REVIEW: should we allow null resources?
            if (resource != null)
            {
                foreach (var req in context.Requirements.OfType<TRequirement>())
                {
                    await HandleAsync(context, req, resource);
                }
            }
        }

        public virtual Task HandleAsync(AuthorizationContext context, TRequirement requirement, TResource resource)
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

        public abstract void Handle(AuthorizationContext context, TRequirement requirement, TResource resource);
    }
}