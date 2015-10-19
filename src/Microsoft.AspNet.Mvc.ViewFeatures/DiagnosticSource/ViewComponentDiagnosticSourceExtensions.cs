// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewEngines;

namespace Microsoft.AspNet.Mvc.Diagnostics
{
    public static class ViewComponentDiagnosticSourceExtensions
    {
        public static void BeforeViewComponent(
            this DiagnosticSource diagnosticSource,
            ViewComponentContext context,
            object viewComponent)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeViewComponent"))
            {
                diagnosticSource.Write(
                "Microsoft.AspNet.Mvc.BeforeViewComponent",
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
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterViewComponent"))
            {
                diagnosticSource.Write(
                "Microsoft.AspNet.Mvc.AfterViewComponent",
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
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.ViewComponentBeforeViewExecute"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.ViewComponentBeforeViewExecute",
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
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.ViewComponentAfterViewExecute"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.ViewComponentAfterViewExecute",
                    new
                    {
                        actionDescriptor = context.ViewContext.ActionDescriptor,
                        viewComponentContext = context,
                        view = view
                    });
            }
        }
    }
}
