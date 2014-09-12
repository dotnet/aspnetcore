// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequireHttpsAttribute :
        Attribute, IAuthorizationFilter, IOrderedFilter
    {
        public int Order { get; set; }

        public virtual void OnAuthorization([NotNull]AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.Request.IsSecure)
            {
                HandleNonHttpsRequest(filterContext);
            }
        }

        protected virtual void HandleNonHttpsRequest(AuthorizationContext filterContext)
        {
            // only redirect for GET requests, otherwise the browser might not propagate the verb and request
            // body correctly.
            if (!string.Equals(filterContext.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = new HttpStatusCodeResult(403);
            }
            else
            {
                var request = filterContext.HttpContext.Request;
                var newUrl = string.Concat(
                    "https://",
                    request.Host.ToUriComponent(),
                    request.PathBase.ToUriComponent(),
                    request.Path.ToUriComponent(),
                    request.QueryString.ToUriComponent());

                // redirect to HTTPS version of page
                filterContext.Result = new RedirectResult(newUrl, permanent: true);
            }
        }
    }
}
