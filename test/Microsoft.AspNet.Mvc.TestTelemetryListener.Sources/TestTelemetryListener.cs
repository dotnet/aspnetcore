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

        public class OnBeforeActionMethodEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IReadOnlyDictionary<string, object> Arguments { get; set; }
        }

        public OnBeforeActionMethodEventData BeforeActionMethod { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.BeforeActionMethod")]
        public virtual void OnBeforeActionMethod(
            IProxyActionContext actionContext,
            IReadOnlyDictionary<string, object> arguments)
        {
            BeforeActionMethod = new OnBeforeActionMethodEventData()
            {
                ActionContext = actionContext,
                Arguments = arguments,
            };
        }

        public class OnAfterActionMethodEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IProxyActionResult Result { get; set; }
        }

        public OnAfterActionMethodEventData AfterActionMethod { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.AfterActionMethod")]
        public virtual void OnAfterActionMethod(
            IProxyActionContext actionContext,
            IProxyActionResult result)
        {
            AfterActionMethod = new OnAfterActionMethodEventData()
            {
                ActionContext = actionContext,
                Result = result,
            };
        }

        public class OnBeforeActionResultEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IProxyActionResult Result { get; set; }
        }

        public OnBeforeActionResultEventData BeforeActionResult { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.BeforeActionResult")]
        public virtual void OnBeforeActionResult(IProxyActionContext actionContext, IProxyActionResult result)
        {
            BeforeActionResult = new OnBeforeActionResultEventData()
            {
                ActionContext = actionContext,
                Result = result,
            };
        }

        public class OnAfterActionResultEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public IProxyActionResult Result { get; set; }
        }

        public OnAfterActionResultEventData AfterActionResult { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.AfterActionResult")]
        public virtual void OnAfterActionResult(IProxyActionContext actionContext, IProxyActionResult result)
        {
            AfterActionResult = new OnAfterActionResultEventData()
            {
                ActionContext = actionContext,
                Result = result,
            };
        }

        public class OnViewFoundEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public bool IsPartial { get; set; }
            public IProxyActionResult Result { get; set; }
            public string ViewName { get; set; }
            public IProxyView View { get; set; }
        }

        public OnViewFoundEventData ViewFound { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.ViewFound")]
        public virtual void OnViewFound(
            IProxyActionContext actionContext,
            bool isPartial,
            IProxyActionResult result,
            string viewName,
            IProxyView view)
        {
           ViewFound = new OnViewFoundEventData()
            {
                ActionContext = actionContext,
                IsPartial = isPartial,
                Result = result,
                ViewName = viewName,
                View = view,
            };
        }

        public class OnViewNotFoundEventData
        {
            public IProxyActionContext ActionContext { get; set; }
            public bool IsPartial { get; set; }
            public IProxyActionResult Result { get; set; }
            public string ViewName { get; set; }
            public IEnumerable<string> SearchedLocations { get; set; }
        }

        public OnViewNotFoundEventData ViewNotFound { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.ViewNotFound")]
        public virtual void OnViewNotFound(
            IProxyActionContext actionContext,
            bool isPartial,
            IProxyActionResult result,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            ViewNotFound = new OnViewNotFoundEventData()
            {
                ActionContext = actionContext,
                IsPartial = isPartial,
                Result = result,
                ViewName = viewName,
                SearchedLocations = searchedLocations,
            };
        }

        public class OnBeforeViewEventData
        {
            public IProxyView View { get; set; }
            public IProxyViewContext ViewContext { get; set; }
        }

        public OnBeforeViewEventData BeforeView { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.BeforeView")]
        public virtual void OnBeforeView(IProxyView view, IProxyViewContext viewContext)
        {
            BeforeView = new OnBeforeViewEventData()
            {
                View = view,
                ViewContext = viewContext,
            };
        }

        public class OnAfterViewEventData
        {
            public IProxyView View { get; set; }
            public IProxyViewContext ViewContext { get; set; }
        }

        public OnAfterViewEventData AfterView { get; set; }

        [TelemetryName("Microsoft.AspNet.Mvc.AfterView")]
        public virtual void OnAfterView(IProxyView view, IProxyViewContext viewContext)
        {
            AfterView = new OnAfterViewEventData()
            {
                View = view,
                ViewContext = viewContext,
            };
        }
    }
}
