// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public static class ViewComponentDiagnosticSourceExtensions
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
    }
}
