// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal static class MvcViewFeaturesDiagnosticListenerExtensions
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeViewComponent.EventName))
            {
                diagnosticListener.Write(
                Diagnostics.BeforeViewComponent.EventName,
                new BeforeViewComponent(
                    context.ViewContext.ActionDescriptor,
                    context,
                    viewComponent
                ));
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterViewComponent.EventName))
            {
                diagnosticListener.Write(
                Diagnostics.AfterViewComponent.EventName,
                new AfterViewComponent(
                    context.ViewContext.ActionDescriptor,
                    context,
                    result,
                    viewComponent
                ));
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
            if (diagnosticListener.IsEnabled(Diagnostics.ViewComponentBeforeViewExecute.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.ViewComponentBeforeViewExecute.EventName,
                    new ViewComponentBeforeViewExecute(
                        context.ViewContext.ActionDescriptor,
                        context,
                        view
                    ));
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
            if (diagnosticListener.IsEnabled(Diagnostics.ViewComponentAfterViewExecute.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.ViewComponentAfterViewExecute.EventName,
                    new ViewComponentAfterViewExecute(
                        context.ViewContext.ActionDescriptor,
                        context,
                        view
                    ));
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeView.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeView.EventName,
                    new BeforeView(view, viewContext));
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterView.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterView.EventName,
                    new AfterView(view, viewContext));
            }
        }

        public static void ViewFound(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            bool isMainPage,
            ActionResult viewResult,
            string viewName,
            IView view)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewFoundImpl(diagnosticListener, actionContext, isMainPage, viewResult, viewName, view);
            }
        }

        private static void ViewFoundImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, bool isMainPage, ActionResult viewResult, string viewName, IView view)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.ViewFound.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.ViewFound.EventName,
                    new ViewFound(
                        actionContext,
                        isMainPage,
                        viewResult,
                        viewName,
                        view
                    ));
            }
        }

        public static void ViewNotFound(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            bool isMainPage,
            ActionResult viewResult,
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                ViewNotFoundImpl(diagnosticListener, actionContext, isMainPage, viewResult, viewName, searchedLocations);
            }
        }

        private static void ViewNotFoundImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, bool isMainPage, ActionResult viewResult, string viewName, IEnumerable<string> searchedLocations)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.ViewNotFound.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.ViewNotFound.EventName,
                    new ViewNotFound(
                        actionContext,
                        isMainPage,
                        viewResult,
                        viewName,
                        searchedLocations
                    ));
            }
        }
    }
}
