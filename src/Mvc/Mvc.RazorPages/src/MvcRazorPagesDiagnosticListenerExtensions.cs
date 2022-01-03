// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

internal static class MvcRazorPagesDiagnosticListenerExtensions
{
    public static void BeforeHandlerMethod(
        this DiagnosticListener diagnosticListener,
        ActionContext actionContext,
        HandlerMethodDescriptor handlerMethodDescriptor,
        IReadOnlyDictionary<string, object?> arguments,
        object instance)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(actionContext != null);
        Debug.Assert(handlerMethodDescriptor != null);
        Debug.Assert(arguments != null);
        Debug.Assert(instance != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeHandlerMethodImpl(diagnosticListener, actionContext, handlerMethodDescriptor, arguments, instance);
        }
    }

    private static void BeforeHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IReadOnlyDictionary<string, object?> arguments, object instance)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforeHandlerMethodEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforeHandlerMethodEventData.EventName,
                new BeforeHandlerMethodEventData(
                    actionContext,
                    arguments,
                    handlerMethodDescriptor,
                    instance
                ));
        }
    }

    public static void AfterHandlerMethod(
        this DiagnosticListener diagnosticListener,
        ActionContext actionContext,
        HandlerMethodDescriptor handlerMethodDescriptor,
        IReadOnlyDictionary<string, object?> arguments,
        object instance,
        IActionResult? result)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(actionContext != null);
        Debug.Assert(handlerMethodDescriptor != null);
        Debug.Assert(arguments != null);
        Debug.Assert(instance != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterHandlerMethodImpl(diagnosticListener, actionContext, handlerMethodDescriptor, arguments, instance, result);
        }
    }

    private static void AfterHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IReadOnlyDictionary<string, object?> arguments, object instance, IActionResult? result)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterHandlerMethodEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterHandlerMethodEventData.EventName,
                new AfterHandlerMethodEventData(
                    actionContext,
                    arguments,
                    handlerMethodDescriptor,
                    instance,
                    result
                ));
        }
    }

    public static void BeforeOnPageHandlerExecution(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutingContext handlerExecutionContext,
        IAsyncPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutionContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeOnPageHandlerExecutionImpl(diagnosticListener, handlerExecutionContext, filter);
        }
    }

    private static void BeforeOnPageHandlerExecutionImpl(DiagnosticListener diagnosticListener, PageHandlerExecutingContext handlerExecutionContext, IAsyncPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforePageFilterOnPageHandlerExecutionEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforePageFilterOnPageHandlerExecutionEventData.EventName,
                new BeforePageFilterOnPageHandlerExecutionEventData(
                    handlerExecutionContext.ActionDescriptor,
                    handlerExecutionContext,
                    filter
                ));
        }
    }

    public static void AfterOnPageHandlerExecution(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutedContext handlerExecutedContext,
        IAsyncPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterOnPageHandlerExecutionImpl(diagnosticListener, handlerExecutedContext, filter);
        }
    }

    private static void AfterOnPageHandlerExecutionImpl(DiagnosticListener diagnosticListener, PageHandlerExecutedContext handlerExecutedContext, IAsyncPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterPageFilterOnPageHandlerExecutionEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterPageFilterOnPageHandlerExecutionEventData.EventName,
                new AfterPageFilterOnPageHandlerExecutionEventData(
                    handlerExecutedContext.ActionDescriptor,
                    handlerExecutedContext,
                    filter
                ));
        }
    }

    public static void BeforeOnPageHandlerExecuting(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutingContext handlerExecutingContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutingContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeOnPageHandlerExecutingImpl(diagnosticListener, handlerExecutingContext, filter);
        }
    }

    private static void BeforeOnPageHandlerExecutingImpl(DiagnosticListener diagnosticListener, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforePageFilterOnPageHandlerExecutingEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforePageFilterOnPageHandlerExecutingEventData.EventName,
                new BeforePageFilterOnPageHandlerExecutingEventData(
                    handlerExecutingContext.ActionDescriptor,
                    handlerExecutingContext,
                    filter
                ));
        }
    }

    public static void AfterOnPageHandlerExecuting(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutingContext handlerExecutingContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutingContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterOnPageHandlerExecutingImpl(diagnosticListener, handlerExecutingContext, filter);
        }
    }

    private static void AfterOnPageHandlerExecutingImpl(DiagnosticListener diagnosticListener, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterPageFilterOnPageHandlerExecutingEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterPageFilterOnPageHandlerExecutingEventData.EventName,
                new AfterPageFilterOnPageHandlerExecutingEventData(
                    handlerExecutingContext.ActionDescriptor,
                    handlerExecutingContext,
                    filter
                ));
        }
    }

    public static void BeforeOnPageHandlerExecuted(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutedContext handlerExecutedContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeOnPageHandlerExecutedImpl(diagnosticListener, handlerExecutedContext, filter);
        }
    }

    private static void BeforeOnPageHandlerExecutedImpl(DiagnosticListener diagnosticListener, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforePageFilterOnPageHandlerExecutedEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforePageFilterOnPageHandlerExecutedEventData.EventName,
                new BeforePageFilterOnPageHandlerExecutedEventData(
                    handlerExecutedContext.ActionDescriptor,
                    handlerExecutedContext,
                    filter
                ));
        }
    }

    public static void AfterOnPageHandlerExecuted(
        this DiagnosticListener diagnosticListener,
        PageHandlerExecutedContext handlerExecutedContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerExecutedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterOnPageHandlerExecutedImpl(diagnosticListener, handlerExecutedContext, filter);
        }
    }

    private static void AfterOnPageHandlerExecutedImpl(DiagnosticListener diagnosticListener, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterPageFilterOnPageHandlerExecutedEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterPageFilterOnPageHandlerExecutedEventData.EventName,
                new AfterPageFilterOnPageHandlerExecutedEventData(
                    handlerExecutedContext.ActionDescriptor,
                    handlerExecutedContext,
                    filter
                ));
        }
    }

    public static void BeforeOnPageHandlerSelection(
        this DiagnosticListener diagnosticListener,
        PageHandlerSelectedContext handlerSelectedContext,
        IAsyncPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerSelectedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeOnPageHandlerSelectionImpl(diagnosticListener, handlerSelectedContext, filter);
        }
    }

    private static void BeforeOnPageHandlerSelectionImpl(DiagnosticListener diagnosticListener, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforePageFilterOnPageHandlerSelectionEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforePageFilterOnPageHandlerSelectionEventData.EventName,
                new BeforePageFilterOnPageHandlerSelectionEventData(
                    handlerSelectedContext.ActionDescriptor,
                    handlerSelectedContext,
                    filter
                ));
        }
    }

    public static void AfterOnPageHandlerSelection(
        this DiagnosticListener diagnosticListener,
        PageHandlerSelectedContext handlerSelectedContext,
        IAsyncPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerSelectedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterOnPageHandlerSelectionImpl(diagnosticListener, handlerSelectedContext, filter);
        }
    }

    private static void AfterOnPageHandlerSelectionImpl(DiagnosticListener diagnosticListener, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterPageFilterOnPageHandlerSelectionEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterPageFilterOnPageHandlerSelectionEventData.EventName,
                new AfterPageFilterOnPageHandlerSelectionEventData(
                    handlerSelectedContext.ActionDescriptor,
                    handlerSelectedContext,
                    filter
                ));
        }
    }

    public static void BeforeOnPageHandlerSelected(
        this DiagnosticListener diagnosticListener,
        PageHandlerSelectedContext handlerSelectedContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerSelectedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            BeforeOnPageHandlerSelectedImpl(diagnosticListener, handlerSelectedContext, filter);
        }
    }

    private static void BeforeOnPageHandlerSelectedImpl(DiagnosticListener diagnosticListener, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.BeforePageFilterOnPageHandlerSelectedEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.BeforePageFilterOnPageHandlerSelectedEventData.EventName,
                new BeforePageFilterOnPageHandlerSelectedEventData(
                    handlerSelectedContext.ActionDescriptor,
                    handlerSelectedContext,
                    filter
                ));
        }
    }

    public static void AfterOnPageHandlerSelected(
        this DiagnosticListener diagnosticListener,
        PageHandlerSelectedContext handlerSelectedContext,
        IPageFilter filter)
    {
        Debug.Assert(diagnosticListener != null);
        Debug.Assert(handlerSelectedContext != null);
        Debug.Assert(filter != null);

        // Inlinable fast-path check if Diagnositcs is enabled
        if (diagnosticListener.IsEnabled())
        {
            AfterOnPageHandlerSelectedImpl(diagnosticListener, handlerSelectedContext, filter);
        }
    }

    private static void AfterOnPageHandlerSelectedImpl(DiagnosticListener diagnosticListener, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
    {
        if (diagnosticListener.IsEnabled(Diagnostics.AfterPageFilterOnPageHandlerSelectedEventData.EventName))
        {
            diagnosticListener.Write(
                Diagnostics.AfterPageFilterOnPageHandlerSelectedEventData.EventName,
                new AfterPageFilterOnPageHandlerSelectedEventData(
                    handlerSelectedContext.ActionDescriptor,
                    handlerSelectedContext,
                    filter
                ));
        }
    }
}
