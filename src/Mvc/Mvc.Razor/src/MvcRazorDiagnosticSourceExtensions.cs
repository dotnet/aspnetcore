// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal static class MvcRazorDiagnosticSourceExtensions
    {
        public static void BeforeViewPage(
            this DiagnosticListener diagnosticListener,
            IRazorPage page,
            ViewContext viewContext)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeViewPageImpl(diagnosticListener, page, viewContext);
            }
        }

        private static void BeforeViewPageImpl(
            this DiagnosticListener diagnosticListener,
            IRazorPage page,
            ViewContext viewContext)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage",
                    new
                    {
                        page = page,
                        viewContext = viewContext,
                        actionDescriptor = viewContext.ActionDescriptor,
                        httpContext = viewContext.HttpContext,
                    });
            }
        }

        public static void AfterViewPage(
            this DiagnosticListener diagnosticListener,
            IRazorPage page,
            ViewContext viewContext)
        {
            // Inlinable fast-path check if Diagnositcs is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterViewPageImpl(diagnosticListener, page, viewContext);
            }
        }

        private static void AfterViewPageImpl(
            this DiagnosticListener diagnosticListener,
            IRazorPage page,
            ViewContext viewContext)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.Razor.AfterViewPage"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.Razor.AfterViewPage",
                    new
                    {
                        page = page,
                        viewContext = viewContext,
                        actionDescriptor = viewContext.ActionDescriptor,
                        httpContext = viewContext.HttpContext,
                    });
            }
        }
    }
}
