// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
    /// </summary>
    public class CreatedAtRouteResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtRouteResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtRouteResult(object routeValues, object value)
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
        public CreatedAtRouteResult(string routeName,
                                    object routeValues,
                                    object value)
            : base(value)
        {
            RouteName = routeName;
            RouteValues = TypeHelper.ObjectToDictionary(routeValues);
            StatusCode = StatusCodes.Status201Created;
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
        public IDictionary<string, object> RouteValues { get; set; }

        /// <inheritdoc />
        protected override void OnFormatting([NotNull] ActionContext context)
        {
            var urlHelper = UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();

            var url = urlHelper.Link(RouteName, RouteValues);

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Headers.Set("Location", url);
        }
    }
}