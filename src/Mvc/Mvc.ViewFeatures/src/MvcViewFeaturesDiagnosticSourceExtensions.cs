// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal static class MvcViewFeaturesDiagnosticSourceExtensions
    {
        public static void BeforeViewComponent(
            this DiagnosticListener diagnosticListener,
            ViewComponentContext context,
            object viewComponent)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeViewComponentImpl(diagnosticListener, context, viewComponent);
            }
        }

        private static void BeforeViewComponentImpl(DiagnosticListener diagnosticListener, ViewComponentContext context, object viewComponent)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeViewComponent"))
            {
                diagnosticListener.Write(
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
            this DiagnosticListener diagnosticListener,
            ViewComponentContext context,
            IViewComponentResult result,
            object viewComponent)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterViewComponentImpl(diagnosticListener, context, result, viewComponent);
            }
        }

        private static void AfterViewComponentImpl(DiagnosticListener diagnosticListener, ViewComponentContext context, IViewComponentResult result, object viewComponent)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterViewComponent"))
            {
                diagnosticListener.Write(
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
            this DiagnosticListener diagnosticListener,
            ViewComponentContext context,
            IView view)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewComponentBeforeViewExecuteImpl(diagnosticListener, context, view);
            }
        }

        private static void ViewComponentBeforeViewExecuteImpl(DiagnosticListener diagnosticListener, ViewComponentContext context, IView view)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute"))
            {
                diagnosticListener.Write(
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
            this DiagnosticListener diagnosticListener,
            ViewComponentContext context,
            IView view)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewComponentAfterViewExecuteImpl(diagnosticListener, context, view);
            }
        }

        private static void ViewComponentAfterViewExecuteImpl(DiagnosticListener diagnosticListener, ViewComponentContext context, IView view)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute"))
            {
                diagnosticListener.Write(
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
            this DiagnosticListener diagnosticListener,
            IView view,
            ViewContext viewContext)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeViewImpl(diagnosticListener, view, viewContext);
            }
        }

        private static void BeforeViewImpl(DiagnosticListener diagnosticListener, IView view, ViewContext viewContext)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeView"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeView",
                    new { view = view, viewContext = viewContext, });
            }
        }

        public static void AfterView(
            this DiagnosticListener diagnosticListener,
            IView view,
            ViewContext viewContext)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterViewImpl(diagnosticListener, view, viewContext);
            }
        }

        private static void AfterViewImpl(DiagnosticListener diagnosticListener, IView view, ViewContext viewContext)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterView"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterView",
                    new { view = view, viewContext = viewContext, });
            }
        }

        public static void ViewFound(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            bool isMainPage,
            PartialViewResult viewResult,
            string viewName,
            IView view)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewFoundImpl(diagnosticListener, actionContext, isMainPage, viewResult, viewName, view);
            }
        }

        private static void ViewFoundImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, bool isMainPage, PartialViewResult viewResult, string viewName, IView view)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.ViewFound"))
            {
                diagnosticListener.Write(
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
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            bool isMainPage,
            PartialViewResult viewResult,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewNotFoundImpl(diagnosticListener, actionContext, isMainPage, viewResult, viewName, searchedLocations);
            }
        }

        private static void ViewNotFoundImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, bool isMainPage, PartialViewResult viewResult, string viewName, IEnumerable<string> searchedLocations)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.ViewNotFound"))
            {
                diagnosticListener.Write(
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
