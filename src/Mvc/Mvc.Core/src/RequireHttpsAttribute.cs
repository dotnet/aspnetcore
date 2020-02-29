// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An authorization filter that confirms requests are received over HTTPS.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequireHttpsAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
    {
        private bool? _permanent;

        /// <summary>
        /// Specifies whether a permanent redirect, <c>301 Moved Permanently</c>,
        /// should be used instead of a temporary redirect, <c>302 Found</c>.
        /// </summary>
        public bool Permanent
        {
            get { return _permanent ?? false; }
            set { _permanent = value; }
        }

        /// <inheritdoc />
        /// <value>Default is <c>int.MinValue + 50</c> to run this <see cref="IAuthorizationFilter"/> early.</value>
        public int Order { get; set; } = int.MinValue + 50;

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized. Confirms requests are received over
        /// HTTPS. Takes no action for HTTPS requests. Otherwise if it was a GET request, sets
        /// <see cref="AuthorizationFilterContext.Result"/> to a result which will redirect the client to the HTTPS
        /// version of the request URI. Otherwise, sets <see cref="AuthorizationFilterContext.Result"/> to a result
        /// which will set the status code to <c>403</c> (Forbidden).
        /// </summary>
        /// <inheritdoc />
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

        /// <summary>
        /// Called from <see cref="OnAuthorization"/> if the request is not received over HTTPS. Expectation is
        /// <see cref="AuthorizationFilterContext.Result"/> will not be <c>null</c> after this method returns.
        /// </summary>
        /// <param name="filterContext">The <see cref="AuthorizationFilterContext"/> to update.</param>
        /// <remarks>
        /// If it was a GET request, default implementation sets <see cref="AuthorizationFilterContext.Result"/> to a
        /// result which will redirect the client to the HTTPS version of the request URI. Otherwise, default
        /// implementation sets <see cref="AuthorizationFilterContext.Result"/> to a result which will set the status
        /// code to <c>403</c> (Forbidden).
        /// </remarks>
        protected virtual void HandleNonHttpsRequest(AuthorizationFilterContext filterContext)
        {
            // only redirect for GET requests, otherwise the browser might not propagate the verb and request
            // body correctly.
            if (!HttpMethods.IsGet(filterContext.HttpContext.Request.Method))
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

                var permanentValue = _permanent ?? optionsAccessor.Value.RequireHttpsPermanent;

                var newUrl = string.Concat(
                    "https://",
                    host.ToUriComponent(),
                    request.PathBase.ToUriComponent(),
                    request.Path.ToUriComponent(),
                    request.QueryString.ToUriComponent());

                // redirect to HTTPS version of page
                filterContext.Result = new RedirectResult(newUrl, permanentValue);
            }
        }
    }
}
