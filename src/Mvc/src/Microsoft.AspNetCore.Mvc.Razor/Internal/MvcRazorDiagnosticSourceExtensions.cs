// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public static class MvcRazorDiagnosticSourceExtensions
    {
        public static void BeforeViewPage(
            this DiagnosticSource diagnosticSource,
            IRazorPage page,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            IRazorPage page,
            ViewContext viewContext)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.Razor.AfterViewPage"))
            {
                diagnosticSource.Write(
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
