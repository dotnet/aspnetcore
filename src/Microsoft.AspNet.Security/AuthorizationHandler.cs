// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    // Music store use case

    // await AuthorizeAsync<Album>(user, "Edit", albumInstance);

    // No policy name needed because this is auto based on resource (operation is the policy name)
    //RegisterOperation which auto generates the policy for Authorize<T>
    //bool AuthorizeAsync<TResource>(ClaimsPrincipal, string operation, TResource instance)
    //bool AuthorizeAsync<TResource>(IAuthorization, ClaimsPrincipal, string operation, TResource instance)
    public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
        where TRequirement : IAuthorizationRequirement
    {
        public async Task HandleAsync(AuthorizationContext context)
        {
            foreach (var req in context.Policy.Requirements.OfType<TRequirement>())
            {
                if (await CheckAsync(context, req))
                {
                    context.Succeed(req);
                }
                else
                {
                    context.Fail();
                }
            }
        }

        public abstract Task<bool> CheckAsync(AuthorizationContext context, TRequirement requirement);
    }

    // TODO: 
    //public abstract class AuthorizationHandler<TRequirement, TResource> : AuthorizationHandler<TRequirement>
    //    where TResource : class
    //    where TRequirement : IAuthorizationRequirement
    //{
    //    public override Task HandleAsync(AuthorizationContext context)
    //    {
    //        var resource = context.Resource as TResource;
    //        if (resource != null)
    //        {
    //            return HandleAsync(context, resource);
    //        }

    //        return Task.FromResult(0);

    //    }

    //    public abstract Task HandleAsync(AuthorizationContext context, TResource resource);
    //}
}