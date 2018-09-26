// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class CreatedAtRouteResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status201Created;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtRouteResult(object routeValues, [ActionResultObjectValue] object value)
            : this(routeName: null, routeValues: routeValues, value: value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtRouteResult(
            string routeName,
            object routeValues,
            [ActionResultObjectValue] object value)
            : base(value)
        {
            RouteName = routeName;
            RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
            StatusCode = DefaultStatusCode;
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <summary>
        /// Gets or sets the name of the route to use for generating the URL.
        /// </summary>
        public string RouteName { get; set; }

        /// <summary>
        /// Gets or sets the route data to use for generating the URL.
        /// </summary>
        public RouteValueDictionary RouteValues { get; set; }

        /// <inheritdoc />
        public override void OnFormatting(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            base.OnFormatting(context);

            var urlHelper = UrlHelper;
            if (urlHelper == null)
            {
                var services = context.HttpContext.RequestServices;
                urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(context);
            }

            var url = urlHelper.Link(RouteName, RouteValues);

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Headers[HeaderNames.Location] = url;
        }
    }
}