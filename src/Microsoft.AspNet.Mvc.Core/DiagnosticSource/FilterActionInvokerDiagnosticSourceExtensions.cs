// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.Diagnostics
{
    public static class FilterActionInvokerDiagnosticSourceExtensions
    {
        public static void BeforeOnAuthorizationAsync(
            this DiagnosticSource diagnosticSource,
            AuthorizationContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnAuthorization",
                    new
                    {
                        actionDescriptor = authorizationContext.ActionDescriptor,
                        authorizationContext = authorizationContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnAuthorizationAsync(
            this DiagnosticSource diagnosticSource,
            AuthorizationContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnAuthorization",
                    new
                    {
                        actionDescriptor = authorizationContext.ActionDescriptor,
                        authorizationContext = authorizationContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnAuthorization(
            this DiagnosticSource diagnosticSource,
            AuthorizationContext authorizationContext,
            IAuthorizationFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnAuthorization",
                    new
                    {
                        actionDescriptor = authorizationContext.ActionDescriptor,
                        authorizationContext = authorizationContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnAuthorization(
            this DiagnosticSource diagnosticSource,
            AuthorizationContext authorizationContext,
            IAuthorizationFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnAuthorization",
                    new
                    {
                        actionDescriptor = authorizationContext.ActionDescriptor,
                        authorizationContext = authorizationContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnResourceExecution(
            this DiagnosticSource diagnosticSource,
            ResourceExecutingContext resourceExecutingContext,
            IAsyncResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResourceExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResourceExecution",
                    new
                    {
                        actionDescriptor = resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext = resourceExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResourceExecution(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResourceExecutedContext resourceExecutedContext,
            IAsyncResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResourceExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResourceExecution",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resourceExecutedContext = resourceExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnResourceExecuting(
            this DiagnosticSource diagnosticSource,
            ResourceExecutingContext resourceExecutingContext,
            IResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResourceExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResourceExecuting",
                    new
                    {
                        actionDescriptor = resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext = resourceExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResourceExecuting(
            this DiagnosticSource diagnosticSource,
            ResourceExecutingContext resourceExecutingContext,
            IResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResourceExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResourceExecuting",
                    new
                    {
                        actionDescriptor = resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext = resourceExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnResourceExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResourceExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResourceExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resourceExecutedContext = resourceExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResourceExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResourceExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResourceExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resourceExecutedContext = resourceExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnExceptionAsync(
            this DiagnosticSource diagnosticSource,
            ExceptionContext exceptionContext,
            IAsyncExceptionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnException",
                    new
                    {
                        actionDescriptor = exceptionContext.ActionDescriptor,
                        exceptionContext = exceptionContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnExceptionAsync(
            this DiagnosticSource diagnosticSource,
            ExceptionContext exceptionContext,
            IAsyncExceptionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnException",
                    new
                    {
                        actionDescriptor = exceptionContext.ActionDescriptor,
                        exceptionContext = exceptionContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnException(
            this DiagnosticSource diagnosticSource,
            ExceptionContext exceptionContext,
            IExceptionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnException",
                    new
                    {
                        actionDescriptor = exceptionContext.ActionDescriptor,
                        exceptionContext = exceptionContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnException(
            this DiagnosticSource diagnosticSource,
            ExceptionContext exceptionContext,
            IExceptionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnException",
                    new
                    {
                        actionDescriptor = exceptionContext.ActionDescriptor,
                        exceptionContext = exceptionContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnActionExecution(
            this DiagnosticSource diagnosticSource,
            ActionExecutingContext actionExecutingContext,
            IAsyncActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnActionExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnActionExecution",
                    new
                    {
                        actionDescriptor = actionExecutingContext.ActionDescriptor,
                        actionExecutingContext = actionExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnActionExecution(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ActionExecutedContext actionExecutedContext,
            IAsyncActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnActionExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnActionExecution",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        actionExecutedContext = actionExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnActionExecuting(
            this DiagnosticSource diagnosticSource,
            ActionExecutingContext actionExecutingContext,
            IActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnActionExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnActionExecuting",
                    new
                    {
                        actionDescriptor = actionExecutingContext.ActionDescriptor,
                        actionExecutingContext = actionExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnActionExecuting(
            this DiagnosticSource diagnosticSource,
            ActionExecutingContext actionExecutingContext,
            IActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnActionExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnActionExecuting",
                    new
                    {
                        actionDescriptor = actionExecutingContext.ActionDescriptor,
                        actionExecutingContext = actionExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnActionExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnActionExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnActionExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        actionExecutedContext = actionExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnActionExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnActionExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnActionExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        actionExecutedContext = actionExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeActionMethod(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IDictionary<string, object> actionArguments,
            object controller)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeActionMethod"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeActionMethod",
                    new
                    {
                        actionContext = actionContext,
                        arguments = actionArguments,
                        controller = controller
                    });
            }
        }

        public static void AfterActionMethod(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IDictionary<string, object> actionArguments,
            object controller,
            IActionResult result)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterActionMethod"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterActionMethod",
                    new
                    {
                        actionContext = actionContext,
                        arguments = actionArguments,
                        controller = controller,
                        result = result
                    });
            }
        }

        public static void BeforeOnResultExecution(
            this DiagnosticSource diagnosticSource,
            ResultExecutingContext resultExecutingContext,
            IAsyncResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResultExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResultExecution",
                    new
                    {
                        actionDescriptor = resultExecutingContext.ActionDescriptor,
                        resultExecutingContext = resultExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResultExecution(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResultExecutedContext resultExecutedContext,
            IAsyncResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResultExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResultExecution",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resultExecutedContext = resultExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnResultExecuting(
            this DiagnosticSource diagnosticSource,
            ResultExecutingContext resultExecutingContext,
            IResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResultExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResultExecuting",
                    new
                    {
                        actionDescriptor = resultExecutingContext.ActionDescriptor,
                        resultExecutingContext = resultExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResultExecuting(
            this DiagnosticSource diagnosticSource,
            ResultExecutingContext resultExecutingContext,
            IResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResultExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResultExecuting",
                    new
                    {
                        actionDescriptor = resultExecutingContext.ActionDescriptor,
                        resultExecutingContext = resultExecutingContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeOnResultExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeOnResultExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeOnResultExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resultExecutedContext = resultExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResultExecuted(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterOnResultExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterOnResultExecuted",
                    new
                    {
                        actionDescriptor = actionDescriptor,
                        resultExecutedContext = resultExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void BeforeActionResult(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IActionResult result)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.BeforeActionResult"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.BeforeActionResult",
                    new { actionContext = actionContext, result = result });
            }
        }

        public static void AfterActionResult(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IActionResult result)
        {
            if (diagnosticSource.IsEnabled("Microsoft.AspNet.Mvc.AfterActionResult"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNet.Mvc.AfterActionResult",
                    new { actionContext = actionContext, result = result });
            }
        }
    }
}
