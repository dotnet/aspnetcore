// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class AuthorizationFilterAttribute :
        Attribute, IAsyncAuthorizationFilter, IAuthorizationFilter, IOrderedFilter
    {
        public int Order { get; set; }

#pragma warning disable 1998
        public virtual async Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
            OnAuthorization(context);
        }
#pragma warning restore 1998

        public virtual void OnAuthorization([NotNull] AuthorizationContext context)
        {
        }

        protected virtual bool HasAllowAnonymous([NotNull] AuthorizationContext context)
        {
            return context.Filters.Any(item => item is IAllowAnonymous);
        }

        protected virtual void Fail([NotNull] AuthorizationContext context)
        {
            context.Result = new HttpStatusCodeResult(StatusCodes.Status401Unauthorized);
        }
    }
}
