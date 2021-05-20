// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal static class MvcRazorDiagnosticListenerExtensions
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeViewPageEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeViewPageEventData.EventName,
                    new BeforeViewPageEventData(
                        page,
                        viewContext,
                        viewContext.ActionDescriptor,
                        viewContext.HttpContext
                    ));
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterViewPageEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterViewPageEventData.EventName,
                    new AfterViewPageEventData(
                        page,
                        viewContext,
                        viewContext.ActionDescriptor,
                        viewContext.HttpContext
                    ));
            }
        }
    }
}