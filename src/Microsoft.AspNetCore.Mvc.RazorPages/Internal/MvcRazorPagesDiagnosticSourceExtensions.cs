// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class MvcRazorPagesDiagnosticSourceExtensions
    {
        public static void BeforeHandlerMethod(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            HandlerMethodDescriptor handlerMethodDescriptor,
            IDictionary<string, object> arguments,
            object instance)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(handlerMethodDescriptor != null);
            Debug.Assert(arguments != null);
            Debug.Assert(instance != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeHandlerMethod"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            HandlerMethodDescriptor handlerMethodDescriptor,
            IDictionary<string, object> arguments,
            object instance,
            IActionResult result)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(handlerMethodDescriptor != null);
            Debug.Assert(arguments != null);
            Debug.Assert(instance != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterHandlerMethod"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutingContext handlerExecutionContext,
            IAsyncPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutionContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecution"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutedContext handlerExecutedContext,
            IAsyncPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecution"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutingContext handlerExecutingContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuting"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutingContext handlerExecutingContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuting"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutedContext handlerExecutedContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecuted"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerExecutedContext handlerExecutedContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecuted"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerSelectedContext handlerSelectedContext,
            IAsyncPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerSelectedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelection"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerSelectedContext handlerSelectedContext,
            IAsyncPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerSelectedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelection"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerSelectedContext handlerSelectedContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerSelectedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerSelected"))
            {
                diagnosticSource.Write(
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
            this DiagnosticSource diagnosticSource,
            PageHandlerSelectedContext handlerSelectedContext,
            IPageFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(handlerSelectedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnPageHandlerSelected"))
            {
                diagnosticSource.Write(
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
