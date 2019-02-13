// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    internal static class MvcViewFeaturesDiagnosticSourceExtensions
    {
        public static void BeforeViewComponent(
            this DiagnosticSource diagnosticSource,
            ViewComponentContext context,
            object viewComponent)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeViewComponent"))
            {
                diagnosticSource.Write(
                "Microsoft.AspNetCore.Mvc.BeforeViewComponent",
                new
                {
                    actionDescriptor = context.ViewContext.ActionDescriptor,
                    viewComponentContext = context,
                    viewComponent = viewComponent
                });
            }
        }

        public static void AfterViewComponent(
            this DiagnosticSource diagnosticSource,
            ViewComponentContext context,
            IViewComponentResult result,
            object viewComponent)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterViewComponent"))
            {
                diagnosticSource.Write(
                "Microsoft.AspNetCore.Mvc.AfterViewComponent",
                new
                {
                    actionDescriptor = context.ViewContext.ActionDescriptor,
                    viewComponentContext = context,
                    viewComponentResult = result,
                    viewComponent = viewComponent
                });
            }
        }

        public static void ViewComponentBeforeViewExecute(
            this DiagnosticSource diagnosticSource,
            ViewComponentContext context,
            IView view)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute",
                    new
                    {
                        actionDescriptor = context.ViewContext.ActionDescriptor,
                        viewComponentContext = context,
                        view = view
                    });
            }
        }

        public static void ViewComponentAfterViewExecute(
            this DiagnosticSource diagnosticSource,
            ViewComponentContext context,
            IView view)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute",
                    new
                    {
                        actionDescriptor = context.ViewContext.ActionDescriptor,
                        viewComponentContext = context,
                        view = view
                    });
            }
        }

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
