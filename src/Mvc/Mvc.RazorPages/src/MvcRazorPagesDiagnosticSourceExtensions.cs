// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal static class MvcRazorPagesDiagnosticSourceExtensions
    {
        public static void BeforeHandlerMethod(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            HandlerMethodDescriptor handlerMethodDescriptor,
            IDictionary<string, object> arguments,
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

        private static void BeforeHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IDictionary<string, object> arguments, object instance)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeHandlerMethod"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeHandlerMethod",
                    new
                    {
                        actionContext = actionContext,
                        arguments = arguments,
                        handlerMethodDescriptor = handlerMethodDescriptor,
                        instance = instance,
                    });
            }
        }

        public static void AfterHandlerMethod(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            HandlerMethodDescriptor handlerMethodDescriptor,
            IDictionary<string, object> arguments,
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

        private static void AfterHandlerMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, HandlerMethodDescriptor handlerMethodDescriptor, IDictionary<string, object> arguments, object instance, IActionResult result)
        {
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterHandlerMethod"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterHandlerMethod",
                    new
                    {
                        actionContext = actionContext,
                        arguments = arguments,
                        handlerMethodDescriptor = handlerMethodDescriptor,
                        instance = instance,
                        result = result
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecution"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecution",
                    new
                    {
                        actionDescriptor = handlerExecutionContext.ActionDescriptor,
                        handlerExecutionContext = handlerExecutionContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecution"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecution",
                    new
                    {
                        actionDescriptor = handlerExecutedContext.ActionDescriptor,
                        handlerExecutedContext = handlerExecutedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuting"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuting",
                    new
                    {
                        actionDescriptor = handlerExecutingContext.ActionDescriptor,
                        handlerExecutingContext = handlerExecutingContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuting"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuting",
                    new
                    {
                        actionDescriptor = handlerExecutingContext.ActionDescriptor,
                        handlerExecutingContext = handlerExecutingContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuted"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuted",
                    new
                    {
                        actionDescriptor = handlerExecutedContext.ActionDescriptor,
                        handlerExecutedContext = handlerExecutedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuted"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuted",
                    new
                    {
                        actionDescriptor = handlerExecutedContext.ActionDescriptor,
                        handlerExecutedContext = handlerExecutedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelection"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelection",
                    new
                    {
                        actionDescriptor = handlerSelectedContext.ActionDescriptor,
                        handlerSelectedContext = handlerSelectedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelection"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelection",
                    new
                    {
                        actionDescriptor = handlerSelectedContext.ActionDescriptor,
                        handlerSelectedContext = handlerSelectedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelected"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelected",
                    new
                    {
                        actionDescriptor = handlerSelectedContext.ActionDescriptor,
                        handlerSelectedContext = handlerSelectedContext,
                        filter = filter
                    });
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
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelected"))
            {
                diagnosticListener.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelected",
                    new
                    {
                        actionDescriptor = handlerSelectedContext.ActionDescriptor,
                        handlerSelectedContext = handlerSelectedContext,
                        filter = filter
                    });
            }
        }
    }
}
