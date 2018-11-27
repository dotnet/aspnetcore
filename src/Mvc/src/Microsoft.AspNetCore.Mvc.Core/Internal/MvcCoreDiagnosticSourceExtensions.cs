// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    // We're doing a lot of asserts here because these methods are really tedious to test and
    // highly dependent on the details of the invoker's state machine. Basically if we wrote the
    // obvious unit tests that would generate a lot of boilerplate and wouldn't cover the hard parts.
    public static class MvcCoreDiagnosticSourceExtensions
    {
        public static void BeforeAction(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionDescriptor != null);
            Debug.Assert(httpContext != null);
            Debug.Assert(routeData != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeAction"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeAction",
                    new { actionDescriptor, httpContext = httpContext, routeData = routeData });
            }
        }

        public static void AfterAction(
            this DiagnosticSource diagnosticSource,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionDescriptor != null);
            Debug.Assert(httpContext != null);
            Debug.Assert(routeData != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterAction"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterAction",
                    new { actionDescriptor, httpContext = httpContext, routeData = routeData });
            }
        }

        public static void BeforeOnAuthorizationAsync(
            this DiagnosticSource diagnosticSource,
            AuthorizationFilterContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnAuthorization",
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
            AuthorizationFilterContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnAuthorization",
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
            AuthorizationFilterContext authorizationContext,
            IAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnAuthorization",
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
            AuthorizationFilterContext authorizationContext,
            IAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnAuthorization"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnAuthorization",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecution",
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
            ResourceExecutedContext resourceExecutedContext,
            IAsyncResourceFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResourceExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResourceExecution",
                    new
                    {
                        actionDescriptor = resourceExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuting",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResourceExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResourceExecuting",
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
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuted",
                    new
                    {
                        actionDescriptor = resourceExecutedContext.ActionDescriptor,
                        resourceExecutedContext = resourceExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResourceExecuted(
            this DiagnosticSource diagnosticSource,
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResourceExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResourceExecuted",
                    new
                    {
                        actionDescriptor = resourceExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnException",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnException",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnException",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnException"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnException",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnActionExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnActionExecution",
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
            ActionExecutedContext actionExecutedContext,
            IAsyncActionFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnActionExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnActionExecution",
                    new
                    {
                        actionDescriptor = actionExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnActionExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnActionExecuting",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnActionExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnActionExecuting",
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
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnActionExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnActionExecuted",
                    new
                    {
                        actionDescriptor = actionExecutedContext.ActionDescriptor,
                        actionExecutedContext = actionExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnActionExecuted(
            this DiagnosticSource diagnosticSource,
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnActionExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnActionExecuted",
                    new
                    {
                        actionDescriptor = actionExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(actionArguments != null);
            Debug.Assert(controller != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeActionMethod"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeActionMethod",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(actionArguments != null);
            Debug.Assert(controller != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterActionMethod"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterActionMethod",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResultExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResultExecution",
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
            ResultExecutedContext resultExecutedContext,
            IAsyncResultFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResultExecution"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResultExecution",
                    new
                    {
                        actionDescriptor = resultExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResultExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResultExecuting",
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResultExecuting"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResultExecuting",
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
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeOnResultExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeOnResultExecuted",
                    new
                    {
                        actionDescriptor = resultExecutedContext.ActionDescriptor,
                        resultExecutedContext = resultExecutedContext,
                        filter = filter
                    });
            }
        }

        public static void AfterOnResultExecuted(
            this DiagnosticSource diagnosticSource,
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterOnResultExecuted"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterOnResultExecuted",
                    new
                    {
                        actionDescriptor = resultExecutedContext.ActionDescriptor,
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
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(result != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeActionResult"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.BeforeActionResult",
                    new { actionContext = actionContext, result = result });
            }
        }

        public static void AfterActionResult(
            this DiagnosticSource diagnosticSource,
            ActionContext actionContext,
            IActionResult result)
        {
            Debug.Assert(diagnosticSource != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(result != null);

            if (diagnosticSource.IsEnabled("Microsoft.AspNetCore.Mvc.AfterActionResult"))
            {
                diagnosticSource.Write(
                    "Microsoft.AspNetCore.Mvc.AfterActionResult",
                    new { actionContext = actionContext, result = result });
            }
        }
    }
}
