// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Notification;

namespace Microsoft.AspNet.Mvc.TestCommon.Notification
{
    public class TestNotificationListener
    {
        public OnActionSelectedEventData ActionSelected { get; set; }

        [NotificationName("Microsoft.AspNet.Mvc.ActionSelected")]
        public virtual void OnActionSelected(
            IHttpContext httpContext,
            IRouteData routeData,
            IActionDescriptor actionDescriptor)
        {
            ActionSelected = new OnActionSelectedEventData()
            {
                ActionDescriptor = actionDescriptor,
                HttpContext = httpContext,
                RouteData = routeData,
            };
        }

        public OnActionInvokedEventData ActionInvoked { get; set; }

        [NotificationName("Microsoft.AspNet.Mvc.ActionInvoked")]
        public virtual void OnActionInvoked(
            IHttpContext httpContext,
            IActionDescriptor actionDescriptor)
        {
            ActionInvoked = new OnActionInvokedEventData()
            {
                ActionDescriptor = actionDescriptor,
                HttpContext = httpContext,
            };
        }

        public class OnActionSelectedEventData
        {
            public IActionDescriptor ActionDescriptor { get; set; }
            public IHttpContext HttpContext { get; set; }
            public IRouteData RouteData { get; set; }
        }

        public class OnActionInvokedEventData
        {
            public IActionDescriptor ActionDescriptor { get; set; }
            public IHttpContext HttpContext { get; set; }
        }
    }
}
