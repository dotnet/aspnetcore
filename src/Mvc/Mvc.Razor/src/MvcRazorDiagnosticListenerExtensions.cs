// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor;

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
