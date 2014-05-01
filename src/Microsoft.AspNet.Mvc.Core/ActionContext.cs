// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionContext
    {
        public ActionContext([NotNull] ActionContext actionContext)
            : this(actionContext.HttpContext, actionContext.RouteData, actionContext.ActionDescriptor)
        {
            ModelState = actionContext.ModelState;
            Controller = actionContext.Controller;
        }

        public ActionContext([NotNull] RouteContext routeContext, [NotNull] ActionDescriptor actionDescriptor)
            : this(routeContext.HttpContext, routeContext.RouteData, actionDescriptor)
        {
        }

        public ActionContext([NotNull] HttpContext httpContext,
            [NotNull] RouteData routeData,
            [NotNull] ActionDescriptor actionDescriptor)
        {
            HttpContext = httpContext;
            RouteData = routeData;
            ActionDescriptor = actionDescriptor;
            ModelState = new ModelStateDictionary();
        }

        public HttpContext HttpContext { get; private set; }

        public RouteData RouteData { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public ActionDescriptor ActionDescriptor { get; private set; }

        /// <summary>
        /// The controller is available only after the controller factory runs.
        /// </summary>
        public object Controller { get; set; }
    }
}
