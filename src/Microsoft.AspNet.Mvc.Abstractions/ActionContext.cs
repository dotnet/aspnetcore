// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
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
        public ActionContext([NotNull] ActionContext actionContext)
            : this(actionContext.HttpContext, actionContext.RouteData, actionContext.ActionDescriptor)
        {
            ModelState = actionContext.ModelState;
        }

        /// <summary>
        /// Creates a new <see cref="ActionContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> for the current request.</param>
        /// <param name="routeData">The <see cref="AspNet.Routing.RouteData"/> for the current request.</param>
        /// <param name="actionDescriptor">The <see cref="Mvc.ActionDescriptor"/> for the selected action.</param>
        public ActionContext(
            [NotNull] HttpContext httpContext,
            [NotNull] RouteData routeData,
            [NotNull] ActionDescriptor actionDescriptor)
            : this(httpContext, routeData, actionDescriptor, new ModelStateDictionary())
        {
        }

        /// <summary>
        /// Creates a new <see cref="ActionContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> for the current request.</param>
        /// <param name="routeData">The <see cref="AspNet.Routing.RouteData"/> for the current request.</param>
        /// <param name="actionDescriptor">The <see cref="Mvc.ActionDescriptor"/> for the selected action.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/>.</param>
        public ActionContext(
            [NotNull] HttpContext httpContext,
            [NotNull] RouteData routeData,
            [NotNull] ActionDescriptor actionDescriptor,
            [NotNull] ModelStateDictionary modelState)
        {
            HttpContext = httpContext;
            RouteData = routeData;
            ActionDescriptor = actionDescriptor;
            ModelState = modelState;
        }

        /// <summary>
        /// Gets or sets the <see cref="Mvc.ActionDescriptor"/> for the selected action.
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
        /// Gets or sets the <see cref="AspNet.Routing.RouteData"/> for the current request.
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