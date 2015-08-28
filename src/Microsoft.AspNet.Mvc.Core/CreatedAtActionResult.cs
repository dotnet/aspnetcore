// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
    /// </summary>
    public class CreatedAtActionResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatedAtActionResult"/> with the values
        /// provided.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to format in the entity body.</param>
        public CreatedAtActionResult(string actionName,
                                     string controllerName,
                                     object routeValues,
                                     object value)
            : base(value)
        {
            ActionName = actionName;
            ControllerName = controllerName;
            RouteValues = PropertyHelper.ObjectToDictionary(routeValues);
            StatusCode = StatusCodes.Status201Created;
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <summary>
        /// Gets or sets the name of the action to use for generating the URL.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the controller to use for generating the URL.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the route data to use for generating the URL.
        /// </summary>
        public IDictionary<string, object> RouteValues { get; set; }

        /// <inheritdoc />
        protected override void OnFormatting([NotNull] ActionContext context)
        {
            var request = context.HttpContext.Request;
            var urlHelper = UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();

            var url = urlHelper.Action(
                ActionName, 
                ControllerName, 
                RouteValues, 
                request.Scheme, 
                request.Host.ToUriComponent());

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Headers[HeaderNames.Location] = url;
        }
    }
}