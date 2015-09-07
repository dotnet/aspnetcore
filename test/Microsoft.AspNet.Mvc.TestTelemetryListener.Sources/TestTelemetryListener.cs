// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.TelemetryAdapter;

namespace Microsoft.AspNet.Mvc
{
    public class TestTelemetryListener
    {
        public class OnBeforeActionEventData
        {
            public IProxyActionDescriptor ActionDescriptor { get; set; }
            public IProxyHttpContext HttpContext { get; set; }
            public IProxyRouteData RouteData { get; set; }
        }

        public OnBeforeActionEventData BeforeAction { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.BeforeAction")]
        public virtual void OnBeforeAction(
            IProxyHttpContext httpContext,
            IProxyRouteData routeData,
            IProxyActionDescriptor actionDescriptor)
        {
            BeforeAction = new OnBeforeActionEventData()
            {
                ActionDescriptor = actionDescriptor,
                HttpContext = httpContext,
                RouteData = routeData,
            };
        }

        public class OnAfterActionEventData
        {
            public IProxyActionDescriptor ActionDescriptor { get; set; }
            public IProxyHttpContext HttpContext { get; set; }
        }

        public OnAfterActionEventData AfterAction { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.AfterAction")]
        public virtual void OnAfterAction(
            IProxyHttpContext httpContext,
            IProxyActionDescriptor actionDescriptor)
        {
            AfterAction = new OnAfterActionEventData()
            {
                ActionDescriptor = actionDescriptor,
                HttpContext = httpContext,
            };
        }

        public class OnViewResultViewFoundEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IProxyActionResult Result { get; set; }
            public string ViewName { get; set; }
            public IProxyView View { get; set; }
        }

        public OnViewResultViewFoundEventData ViewResultViewFound { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.ViewResultViewFound")]
        public virtual void OnViewResultViewFound(
            IProxyActionContext actionContext,
            IProxyActionResult result,
            string viewName,
            IProxyView view)
        {
            ViewResultViewFound = new OnViewResultViewFoundEventData()
            {
                ActionContext = actionContext,
                Result = result,
                ViewName = viewName,
                View = view,
            };
        }

        public class OnViewResultViewNotFoundEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IProxyActionResult Result { get; set; }
            public string ViewName { get; set; }
            public IEnumerable<string> SearchedLocations { get; set; }
        }

        public OnViewResultViewNotFoundEventData ViewResultViewNotFound { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.ViewResultViewNotFound")]
        public virtual void OnViewResultViewNotFound(
            IProxyActionContext actionContext,
            IProxyActionResult result,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            ViewResultViewNotFound = new OnViewResultViewNotFoundEventData()
            {
                ActionContext = actionContext,
                Result = result,
                ViewName = viewName,
                SearchedLocations = searchedLocations,
            };
        }
    }
}
