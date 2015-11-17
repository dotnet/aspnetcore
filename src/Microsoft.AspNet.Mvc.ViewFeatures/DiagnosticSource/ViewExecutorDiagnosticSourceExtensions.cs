// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;

namespace Microsoft.AspNet.Mvc.Diagnostics
{
    public static class ViewExecutorDiagnosticSourceExtensions
    {
        public static void BeforeView(
            this DiagnosticSource diagnosticSource,
            IView view,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeView"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeView",
                    new { view = view, viewContext = viewContext, });
            }
        }

        public static void AfterView(
            this DiagnosticSource diagnosticSource,
            IView view,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterView"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterView",
                    new { view = view, viewContext = viewContext, });
            }
        }

        public static void ViewFound(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            bool isMainPage,
            PartialViewResult viewResult,
            string viewName,
            IView view)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.ViewFound"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.ViewFound",
                    new
                    {
                        actionContext = actionContext,
                        isMainPage = isMainPage,
                        result = viewResult,
                        viewName = viewName,
                        view = view,
                    });
            }
        }

        public static void ViewNotFound(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            bool isMainPage,
            PartialViewResult viewResult,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.ViewNotFound"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.ViewNotFound",
                    new
                    {
                        actionContext = actionContext,
                        isMainPage = isMainPage,
                        result = viewResult,
                        viewName = viewName,
                        searchedLocations = searchedLocations,
                    });
            }
        }
    }
}