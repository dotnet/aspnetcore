// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using ShimResources = Microsoft.AspNet.Mvc.WebApiCompatShim.Resources;

namespace System.Web.Http
{
    /// <summary>
    /// Represents an action result that performs route generation and content negotiation and returns a
    /// <see cref="HttpStatusCode.Created"/> response when content negotiation succeeds.
    /// </summary>
    /// <typeparam name="T">The type of content in the entity body.</typeparam>
    public class CreatedAtRouteNegotiatedContentResult<T> : NegotiatedContentResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteNegotiatedContentResult{T}"/> class with the
        /// values provided.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        /// <param name="formatters">The formatters to use to negotiate and format the content.</param>
        public CreatedAtRouteNegotiatedContentResult(
            [NotNull] string routeName, 
            [NotNull] IDictionary<string, object> routeValues,
            [NotNull] T content)
            : base(HttpStatusCode.Created, content)
        {
            RouteName = routeName;
            RouteValues = routeValues;
        }

        /// <summary>
        /// Gets the name of the route to use for generating the URL.
        /// </summary>
        public string RouteName { get; private set; }

        /// <summary>
        /// Gets the route data to use for generating the URL.
        /// </summary>
        public IDictionary<string, object> RouteValues { get; private set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            var request = context.HttpContext.Request;
            var urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelper>();

            var url = urlHelper.RouteUrl(RouteName, RouteValues, request.Scheme, request.Host.ToUriComponent());
            if (url == null)
            {
                throw new InvalidOperationException(ShimResources.FormatCreatedAtRoute_RouteFailed(RouteName));
            }

            context.HttpContext.Response.Headers.Add("Location", new string[] { url });

            return base.ExecuteResultAsync(context);
        }
    }
}