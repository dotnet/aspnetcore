// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Context object for execution of action which has been selected as part of an HTTP request.
    /// </summary>
    public class ActionContext
    {
        /// <summary>
        /// Creates an empty <see cref="ActionContext"/>.
        /// </summary>
        /// <remarks>
        /// The default constructor is provided for unit test purposes only.
        /// </remarks>
        public ActionContext()
        {
            ModelState = new ModelStateDictionary();
        }

        /// <summary>
        /// Creates a new <see cref="ActionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> to copy.</param>
        public ActionContext(ActionContext actionContext)
            : this(
                actionContext?.HttpContext,
                actionContext?.RouteData,
                actionContext?.ActionDescriptor,
                actionContext?.ModelState)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ActionContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> for the current request.</param>
        /// <param name="routeData">The <see cref="AspNetCore.Routing.RouteData"/> for the current request.</param>
        /// <param name="actionDescriptor">The <see cref="Abstractions.ActionDescriptor"/> for the selected action.</param>
        public ActionContext(
            HttpContext httpContext,
            RouteData routeData,
            ActionDescriptor actionDescriptor)
            : this(httpContext, routeData, actionDescriptor, new ModelStateDictionary())
        {
        }

        /// <summary>
        /// Creates a new <see cref="ActionContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> for the current request.</param>
        /// <param name="routeData">The <see cref="AspNetCore.Routing.RouteData"/> for the current request.</param>
        /// <param name="actionDescriptor">The <see cref="Abstractions.ActionDescriptor"/> for the selected action.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/>.</param>
        public ActionContext(
            HttpContext httpContext,
            RouteData routeData,
            ActionDescriptor actionDescriptor,
            ModelStateDictionary modelState)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }

            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            HttpContext = httpContext;
            RouteData = routeData;
            ActionDescriptor = actionDescriptor;
            ModelState = modelState;
        }

        /// <summary>
        /// Gets or sets the <see cref="Abstractions.ActionDescriptor"/> for the selected action.
        /// </summary>
        /// <remarks>
        /// The property setter is provided for unit test purposes only.
        /// </remarks>
        public ActionDescriptor ActionDescriptor
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the <see cref="Http.HttpContext"/> for the current request.
        /// </summary>
        /// <remarks>
        /// The property setter is provided for unit test purposes only.
        /// </remarks>
        public HttpContext HttpContext
        {
            get; set;
        }

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get;
        }

        /// <summary>
        /// Gets or sets the <see cref="AspNetCore.Routing.RouteData"/> for the current request.
        /// </summary>
        /// <remarks>
        /// The property setter is provided for unit test purposes only.
        /// </remarks>
        public RouteData RouteData
        {
            get; set;
        }
    }
}
