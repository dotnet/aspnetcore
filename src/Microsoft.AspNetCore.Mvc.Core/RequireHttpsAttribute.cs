// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequireHttpsAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
    {
        public int Order { get; set; }

        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            if (!filterContext.HttpContext.Request.IsHttps)
            {
                HandleNonHttpsRequest(filterContext);
            }
        }

        protected virtual void HandleNonHttpsRequest(AuthorizationFilterContext filterContext)
        {
            // only redirect for GET requests, otherwise the browser might not propagate the verb and request
            // body correctly.
            if (!string.Equals(filterContext.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
            else
            {
                var optionsAccessor = filterContext.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();

                var request = filterContext.HttpContext.Request;

                var host = request.Host;
                if (optionsAccessor.Value.SslPort.HasValue && optionsAccessor.Value.SslPort > 0)
                {
                    // a specific SSL port is specified
                    host = new HostString(host.Host, optionsAccessor.Value.SslPort.Value);
                }
                else
                {
                    // clear the port
                    host = new HostString(host.Host);
                }
                
                var newUrl = string.Concat(
                    "https://",
                    host.ToUriComponent(),
                    request.PathBase.ToUriComponent(),
                    request.Path.ToUriComponent(),
                    request.QueryString.ToUriComponent());

                // redirect to HTTPS version of page
                filterContext.Result = new RedirectResult(newUrl, permanent: true);
            }
        }
    }
}
