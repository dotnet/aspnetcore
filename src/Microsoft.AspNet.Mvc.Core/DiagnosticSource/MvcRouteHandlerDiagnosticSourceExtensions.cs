// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Diagnostics
{
    public static class MvcRouteHandlerDiagnosticSourceExtensions
    {
        public static void BeforeAction(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeAction"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeAction",
                    new { actionDescriptor, httpContext = httpContext, routeData = routeData });
            }
        }

        public static void AfterAction(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterAction"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterAction",
                    new { actionDescriptor, httpContext = httpContext, routeData = routeData });
            }
        }
    }
}
