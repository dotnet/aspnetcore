// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal static class MvcRazorPagesDiagnosticListenerExtensions
    {
        public static void BeforeHandlerMethod(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            HandlerMethodDescriptor handlerMethodDescriptor,
            IReadOnlyDictionary<string, object> arguments,
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

        private static void BeforeHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IReadOnlyDictionary<string, object> arguments, object instance)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeHandlerMethod.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeHandlerMethod.EventName,
                    new BeforeHandlerMethod(
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
            IReadOnlyDictionary<string, object> arguments,
            object instance,
            IActionResult result)
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

        private static void AfterHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IReadOnlyDictionary<string, object> arguments, object instance, IActionResult result)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterHandlerMethod.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterHandlerMethod.EventName,
                    new AfterHandlerMethod(
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeOnPageHandlerExecution.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeOnPageHandlerExecution.EventName,
                    new BeforeOnPageHandlerExecution(
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterOnPageHandlerExecution.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterOnPageHandlerExecution.EventName,
                    new AfterOnPageHandlerExecution(
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeOnPageHandlerExecuting.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeOnPageHandlerExecuting.EventName,
                    new BeforeOnPageHandlerExecuting(
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterOnPageHandlerExecuting.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterOnPageHandlerExecuting.EventName,
                    new AfterOnPageHandlerExecuting(
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeOnPageHandlerExecuted.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeOnPageHandlerExecuted.EventName,
                    new BeforeOnPageHandlerExecuted(
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterOnPageHandlerExecuted.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterOnPageHandlerExecuted.EventName,
                    new AfterOnPageHandlerExecuted(
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeOnPageHandlerSelection.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeOnPageHandlerSelection.EventName,
                    new BeforeOnPageHandlerSelection(
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterOnPageHandlerSelection.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterOnPageHandlerSelection.EventName,
                    new AfterOnPageHandlerSelection(
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
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeOnPageHandlerSelected.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeOnPageHandlerSelected.EventName,
                    new BeforeOnPageHandlerSelected(
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
            if (diagnosticListener.IsEnabled(Diagnostics.AfterOnPageHandlerSelected.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterOnPageHandlerSelected.EventName,
                    new AfterOnPageHandlerSelected(
                        handlerSelectedContext.ActionDescriptor,
                        handlerSelectedContext,
                        filter
                    ));
            }
        }
    }
}
