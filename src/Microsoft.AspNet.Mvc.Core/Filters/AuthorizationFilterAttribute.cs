// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class AuthorizationFilterAttribute : Attribute, IAsyncAuthorizationFilter, IAuthorizationFilter, IOrderedFilter
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
            context.Result = new HttpStatusCodeResult(401);
        }
    }
}
