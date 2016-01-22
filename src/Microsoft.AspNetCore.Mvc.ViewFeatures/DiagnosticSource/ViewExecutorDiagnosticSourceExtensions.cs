// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public static class ViewExecutorDiagnosticSourceExtensions
    {
        public static void BeforeView(
            this DiagnosticSource diagnosticSource,
            IView view,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeView"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeView",
                    new { view = view, viewContext = viewContext, });
            }
        }

        public static void AfterView(
            this DiagnosticSource diagnosticSource,
            IView view,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterView"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterView",
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
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewFound"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.ViewFound",
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
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewNotFound"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.ViewNotFound",
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