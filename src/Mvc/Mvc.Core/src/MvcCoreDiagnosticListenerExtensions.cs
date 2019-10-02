// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    // We're doing a lot of asserts here because these methods are really tedious to test and
    // highly dependent on the details of the invoker's state machine. Basically if we wrote the
    // obvious unit tests that would generate a lot of boilerplate and wouldn't cover the hard parts.
    internal static class MvcCoreDiagnosticListenerExtensions
    {
        public static void BeforeAction(
            this DiagnosticListener diagnosticListener,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionDescriptor != null);
            Debug.Assert(httpContext != null);
            Debug.Assert(routeData != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeActionImpl(diagnosticListener, actionDescriptor, httpContext, routeData);
            }
        }

        private static void BeforeActionImpl(DiagnosticListener diagnosticListener, ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeActionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeActionEventData.EventName,
                    new BeforeActionEventData(actionDescriptor, httpContext, routeData));
            }
        }

        public static void AfterAction(
            this DiagnosticListener diagnosticListener,
            ActionDescriptor actionDescriptor,
            HttpContext httpContext,
            RouteData routeData)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionDescriptor != null);
            Debug.Assert(httpContext != null);
            Debug.Assert(routeData != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterActionImpl(diagnosticListener, actionDescriptor, httpContext, routeData);
            }
        }

        private static void AfterActionImpl(DiagnosticListener diagnosticListener, ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterActionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterActionEventData.EventName,
                    new AfterActionEventData(actionDescriptor, httpContext, routeData));
            }
        }

        public static void BeforeOnAuthorizationAsync(
            this DiagnosticListener diagnosticListener,
            AuthorizationFilterContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnAuthorizationAsyncImpl(diagnosticListener, authorizationContext, filter);
            }
        }

        private static void BeforeOnAuthorizationAsyncImpl(DiagnosticListener diagnosticListener, AuthorizationFilterContext authorizationContext, IAsyncAuthorizationFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeAuthorizationFilterOnAuthorizationEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeAuthorizationFilterOnAuthorizationEventData.EventName,
                    new BeforeAuthorizationFilterOnAuthorizationEventData(
                        authorizationContext.ActionDescriptor,
                        authorizationContext,
                        filter
                    ));
            }
        }

        public static void AfterOnAuthorizationAsync(
            this DiagnosticListener diagnosticListener,
            AuthorizationFilterContext authorizationContext,
            IAsyncAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnAuthorizationAsyncImpl(diagnosticListener, authorizationContext, filter);
            }
        }

        private static void AfterOnAuthorizationAsyncImpl(DiagnosticListener diagnosticListener, AuthorizationFilterContext authorizationContext, IAsyncAuthorizationFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterAuthorizationFilterOnAuthorizationEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterAuthorizationFilterOnAuthorizationEventData.EventName,
                    new AfterAuthorizationFilterOnAuthorizationEventData(
                        authorizationContext.ActionDescriptor,
                        authorizationContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnAuthorization(
            this DiagnosticListener diagnosticListener,
            AuthorizationFilterContext authorizationContext,
            IAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnAuthorizationImpl(diagnosticListener, authorizationContext, filter);
            }
        }

        private static void BeforeOnAuthorizationImpl(DiagnosticListener diagnosticListener, AuthorizationFilterContext authorizationContext, IAuthorizationFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeAuthorizationFilterOnAuthorizationEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeAuthorizationFilterOnAuthorizationEventData.EventName,
                    new BeforeAuthorizationFilterOnAuthorizationEventData(
                        authorizationContext.ActionDescriptor,
                        authorizationContext,
                        filter
                    ));
            }
        }

        public static void AfterOnAuthorization(
            this DiagnosticListener diagnosticListener,
            AuthorizationFilterContext authorizationContext,
            IAuthorizationFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(authorizationContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnAuthorizationImpl(diagnosticListener, authorizationContext, filter);
            }
        }

        private static void AfterOnAuthorizationImpl(DiagnosticListener diagnosticListener, AuthorizationFilterContext authorizationContext, IAuthorizationFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterAuthorizationFilterOnAuthorizationEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterAuthorizationFilterOnAuthorizationEventData.EventName,
                    new AfterAuthorizationFilterOnAuthorizationEventData(
                        authorizationContext.ActionDescriptor,
                        authorizationContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnResourceExecution(
            this DiagnosticListener diagnosticListener,
            ResourceExecutingContext resourceExecutingContext,
            IAsyncResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResourceExecutionImpl(diagnosticListener, resourceExecutingContext, filter);
            }
        }

        private static void BeforeOnResourceExecutionImpl(DiagnosticListener diagnosticListener, ResourceExecutingContext resourceExecutingContext, IAsyncResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResourceFilterOnResourceExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResourceFilterOnResourceExecutionEventData.EventName,
                    new BeforeResourceFilterOnResourceExecutionEventData(
                        resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResourceExecution(
            this DiagnosticListener diagnosticListener,
            ResourceExecutedContext resourceExecutedContext,
            IAsyncResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResourceExecutionImpl(diagnosticListener, resourceExecutedContext, filter);
            }
        }

        private static void AfterOnResourceExecutionImpl(DiagnosticListener diagnosticListener, ResourceExecutedContext resourceExecutedContext, IAsyncResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResourceFilterOnResourceExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResourceFilterOnResourceExecutionEventData.EventName,
                    new AfterResourceFilterOnResourceExecutionEventData(
                        resourceExecutedContext.ActionDescriptor,
                        resourceExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnResourceExecuting(
            this DiagnosticListener diagnosticListener,
            ResourceExecutingContext resourceExecutingContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResourceExecutingImpl(diagnosticListener, resourceExecutingContext, filter);
            }
        }

        private static void BeforeOnResourceExecutingImpl(DiagnosticListener diagnosticListener, ResourceExecutingContext resourceExecutingContext, IResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResourceFilterOnResourceExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResourceFilterOnResourceExecutingEventData.EventName,
                    new BeforeResourceFilterOnResourceExecutingEventData(
                        resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResourceExecuting(
            this DiagnosticListener diagnosticListener,
            ResourceExecutingContext resourceExecutingContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResourceExecutingImpl(diagnosticListener, resourceExecutingContext, filter);
            }
        }

        private static void AfterOnResourceExecutingImpl(DiagnosticListener diagnosticListener, ResourceExecutingContext resourceExecutingContext, IResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResourceFilterOnResourceExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResourceFilterOnResourceExecutingEventData.EventName,
                    new AfterResourceFilterOnResourceExecutingEventData(
                        resourceExecutingContext.ActionDescriptor,
                        resourceExecutingContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnResourceExecuted(
            this DiagnosticListener diagnosticListener,
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResourceExecutedImpl(diagnosticListener, resourceExecutedContext, filter);
            }
        }

        private static void BeforeOnResourceExecutedImpl(DiagnosticListener diagnosticListener, ResourceExecutedContext resourceExecutedContext, IResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResourceFilterOnResourceExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResourceFilterOnResourceExecutedEventData.EventName,
                    new BeforeResourceFilterOnResourceExecutedEventData(
                        resourceExecutedContext.ActionDescriptor,
                        resourceExecutedContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResourceExecuted(
            this DiagnosticListener diagnosticListener,
            ResourceExecutedContext resourceExecutedContext,
            IResourceFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resourceExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResourceExecutedImpl(diagnosticListener, resourceExecutedContext, filter);
            }
        }

        private static void AfterOnResourceExecutedImpl(DiagnosticListener diagnosticListener, ResourceExecutedContext resourceExecutedContext, IResourceFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResourceFilterOnResourceExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResourceFilterOnResourceExecutedEventData.EventName,
                    new AfterResourceFilterOnResourceExecutedEventData(
                        resourceExecutedContext.ActionDescriptor,
                        resourceExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnExceptionAsync(
            this DiagnosticListener diagnosticListener,
            ExceptionContext exceptionContext,
            IAsyncExceptionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnExceptionAsyncImpl(diagnosticListener, exceptionContext, filter);
            }
        }

        private static void BeforeOnExceptionAsyncImpl(DiagnosticListener diagnosticListener, ExceptionContext exceptionContext, IAsyncExceptionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeExceptionFilterOnException.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeExceptionFilterOnException.EventName,
                    new BeforeExceptionFilterOnException(
                        exceptionContext.ActionDescriptor,
                        exceptionContext,
                        filter
                    ));
            }
        }

        public static void AfterOnExceptionAsync(
            this DiagnosticListener diagnosticListener,
            ExceptionContext exceptionContext,
            IAsyncExceptionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnExceptionAsyncImpl(diagnosticListener, exceptionContext, filter);
            }
        }

        private static void AfterOnExceptionAsyncImpl(DiagnosticListener diagnosticListener, ExceptionContext exceptionContext, IAsyncExceptionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterExceptionFilterOnExceptionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterExceptionFilterOnExceptionEventData.EventName,
                    new AfterExceptionFilterOnExceptionEventData(
                        exceptionContext.ActionDescriptor,
                        exceptionContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnException(
            this DiagnosticListener diagnosticListener,
            ExceptionContext exceptionContext,
            IExceptionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnExceptionImpl(diagnosticListener, exceptionContext, filter);
            }
        }

        private static void BeforeOnExceptionImpl(DiagnosticListener diagnosticListener, ExceptionContext exceptionContext, IExceptionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeExceptionFilterOnException.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeExceptionFilterOnException.EventName,
                    new BeforeExceptionFilterOnException(
                        exceptionContext.ActionDescriptor,
                        exceptionContext,
                        filter
                    ));
            }
        }

        public static void AfterOnException(
            this DiagnosticListener diagnosticListener,
            ExceptionContext exceptionContext,
            IExceptionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(exceptionContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnExceptionImpl(diagnosticListener, exceptionContext, filter);
            }
        }

        private static void AfterOnExceptionImpl(DiagnosticListener diagnosticListener, ExceptionContext exceptionContext, IExceptionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterExceptionFilterOnExceptionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterExceptionFilterOnExceptionEventData.EventName,
                    new AfterExceptionFilterOnExceptionEventData(
                        exceptionContext.ActionDescriptor,
                        exceptionContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnActionExecution(
            this DiagnosticListener diagnosticListener,
            ActionExecutingContext actionExecutingContext,
            IAsyncActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnActionExecutionImpl(diagnosticListener, actionExecutingContext, filter);
            }
        }

        private static void BeforeOnActionExecutionImpl(DiagnosticListener diagnosticListener, ActionExecutingContext actionExecutingContext, IAsyncActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeActionFilterOnActionExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeActionFilterOnActionExecutionEventData.EventName,
                    new BeforeActionFilterOnActionExecutionEventData(
                        actionExecutingContext.ActionDescriptor,
                        actionExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnActionExecution(
            this DiagnosticListener diagnosticListener,
            ActionExecutedContext actionExecutedContext,
            IAsyncActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnActionExecutionImpl(diagnosticListener, actionExecutedContext, filter);
            }
        }

        private static void AfterOnActionExecutionImpl(DiagnosticListener diagnosticListener, ActionExecutedContext actionExecutedContext, IAsyncActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterActionFilterOnActionExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterActionFilterOnActionExecutionEventData.EventName,
                    new AfterActionFilterOnActionExecutionEventData(
                        actionExecutedContext.ActionDescriptor,
                        actionExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnActionExecuting(
            this DiagnosticListener diagnosticListener,
            ActionExecutingContext actionExecutingContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnActionExecutingImpl(diagnosticListener, actionExecutingContext, filter);
            }
        }

        private static void BeforeOnActionExecutingImpl(DiagnosticListener diagnosticListener, ActionExecutingContext actionExecutingContext, IActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeActionFilterOnActionExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeActionFilterOnActionExecutingEventData.EventName,
                    new BeforeActionFilterOnActionExecutingEventData(
                        actionExecutingContext.ActionDescriptor,
                        actionExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnActionExecuting(
            this DiagnosticListener diagnosticListener,
            ActionExecutingContext actionExecutingContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnActionExecutingImpl(diagnosticListener, actionExecutingContext, filter);
            }
        }

        private static void AfterOnActionExecutingImpl(DiagnosticListener diagnosticListener, ActionExecutingContext actionExecutingContext, IActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterActionFilterOnActionExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterActionFilterOnActionExecutingEventData.EventName,
                    new AfterActionFilterOnActionExecutingEventData(
                        actionExecutingContext.ActionDescriptor,
                        actionExecutingContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnActionExecuted(
            this DiagnosticListener diagnosticListener,
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnActionExecutedImpl(diagnosticListener, actionExecutedContext, filter);
            }
        }

        private static void BeforeOnActionExecutedImpl(DiagnosticListener diagnosticListener, ActionExecutedContext actionExecutedContext, IActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeActionFilterOnActionExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeActionFilterOnActionExecutedEventData.EventName,
                    new BeforeActionFilterOnActionExecutedEventData(
                        actionExecutedContext.ActionDescriptor,
                        actionExecutedContext,
                        filter
                    ));
            }
        }

        public static void AfterOnActionExecuted(
            this DiagnosticListener diagnosticListener,
            ActionExecutedContext actionExecutedContext,
            IActionFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnActionExecutedImpl(diagnosticListener, actionExecutedContext, filter);
            }
        }

        private static void AfterOnActionExecutedImpl(DiagnosticListener diagnosticListener, ActionExecutedContext actionExecutedContext, IActionFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterActionFilterOnActionExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterActionFilterOnActionExecutedEventData.EventName,
                    new AfterActionFilterOnActionExecutedEventData(
                        actionExecutedContext.ActionDescriptor,
                        actionExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeControllerActionMethod(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            IReadOnlyDictionary<string, object> actionArguments,
            object controller)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(actionArguments != null);
            Debug.Assert(controller != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeActionMethodImpl(diagnosticListener, actionContext, actionArguments, controller);
            }
        }

        private static void BeforeActionMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, IReadOnlyDictionary<string, object> actionArguments, object controller)
        {
            // Intellitrace uses this in VS, so we fire the old event with camelCase event names.
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.BeforeActionMethod"))
            {
                diagnosticListener.Write(
                 "Microsoft.AspNetCore.Mvc.BeforeActionMethod",
                 new
                 {
                     actionContext,
                     actionArguments,
                     controller
                 });
            }

            if (diagnosticListener.IsEnabled(Diagnostics.BeforeControllerActionMethodEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeControllerActionMethodEventData.EventName,
                    new BeforeControllerActionMethodEventData(
                        actionContext,
                        actionArguments,
                        controller
                    ));
            }
        }

        public static void AfterControllerActionMethod(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            IReadOnlyDictionary<string, object> actionArguments,
            object controller,
            IActionResult result)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(actionArguments != null);
            Debug.Assert(controller != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterActionMethodImpl(diagnosticListener, actionContext, actionArguments, controller, result);
            }
        }

        private static void AfterActionMethodImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, IReadOnlyDictionary<string, object> actionArguments, object controller, IActionResult result)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterControllerActionMethodEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterControllerActionMethodEventData.EventName,
                    new AfterControllerActionMethodEventData(
                        actionContext,
                        actionArguments,
                        controller,
                        result
                    ));
            }

            // Firing the old event for compat.
            if (diagnosticListener.IsEnabled("Microsoft.AspNetCore.Mvc.AfterActionMethod"))
            {
                diagnosticListener.Write(
                 "Microsoft.AspNetCore.Mvc.AfterActionMethod",
                 new
                 {
                     actionContext,
                     actionArguments,
                     controller,
                     result
                 });
            }
        }

        public static void BeforeOnResultExecution(
            this DiagnosticListener diagnosticListener,
            ResultExecutingContext resultExecutingContext,
            IAsyncResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResultExecutionImpl(diagnosticListener, resultExecutingContext, filter);
            }
        }

        private static void BeforeOnResultExecutionImpl(DiagnosticListener diagnosticListener, ResultExecutingContext resultExecutingContext, IAsyncResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResultFilterOnResultExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResultFilterOnResultExecutionEventData.EventName,
                    new BeforeResultFilterOnResultExecutionEventData(
                        resultExecutingContext.ActionDescriptor,
                        resultExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResultExecution(
            this DiagnosticListener diagnosticListener,
            ResultExecutedContext resultExecutedContext,
            IAsyncResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResultExecutionImpl(diagnosticListener, resultExecutedContext, filter);
            }
        }

        private static void AfterOnResultExecutionImpl(DiagnosticListener diagnosticListener, ResultExecutedContext resultExecutedContext, IAsyncResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResultFilterOnResultExecutionEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResultFilterOnResultExecutionEventData.EventName,
                    new AfterResultFilterOnResultExecutionEventData(
                        resultExecutedContext.ActionDescriptor,
                        resultExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnResultExecuting(
            this DiagnosticListener diagnosticListener,
            ResultExecutingContext resultExecutingContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResultExecutingImpl(diagnosticListener, resultExecutingContext, filter);
            }
        }

        private static void BeforeOnResultExecutingImpl(DiagnosticListener diagnosticListener, ResultExecutingContext resultExecutingContext, IResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResultFilterOnResultExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResultFilterOnResultExecutingEventData.EventName,
                    new BeforeResultFilterOnResultExecutingEventData(
                        resultExecutingContext.ActionDescriptor,
                        resultExecutingContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResultExecuting(
            this DiagnosticListener diagnosticListener,
            ResultExecutingContext resultExecutingContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutingContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResultExecutingImpl(diagnosticListener, resultExecutingContext, filter);
            }
        }

        private static void AfterOnResultExecutingImpl(DiagnosticListener diagnosticListener, ResultExecutingContext resultExecutingContext, IResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResultFilterOnResultExecutingEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResultFilterOnResultExecutingEventData.EventName,
                    new AfterResultFilterOnResultExecutingEventData(
                        resultExecutingContext.ActionDescriptor,
                        resultExecutingContext,
                        filter
                    ));
            }
        }

        public static void BeforeOnResultExecuted(
            this DiagnosticListener diagnosticListener,
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeOnResultExecutedImpl(diagnosticListener, resultExecutedContext, filter);
            }
        }

        private static void BeforeOnResultExecutedImpl(DiagnosticListener diagnosticListener, ResultExecutedContext resultExecutedContext, IResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeResultFilterOnResultExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeResultFilterOnResultExecutedEventData.EventName,
                    new BeforeResultFilterOnResultExecutedEventData(
                        resultExecutedContext.ActionDescriptor,
                        resultExecutedContext,
                        filter
                    ));
            }
        }

        public static void AfterOnResultExecuted(
            this DiagnosticListener diagnosticListener,
            ResultExecutedContext resultExecutedContext,
            IResultFilter filter)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(resultExecutedContext != null);
            Debug.Assert(filter != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterOnResultExecutedImpl(diagnosticListener, resultExecutedContext, filter);
            }
        }

        private static void AfterOnResultExecutedImpl(DiagnosticListener diagnosticListener, ResultExecutedContext resultExecutedContext, IResultFilter filter)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterResultFilterOnResultExecutedEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterResultFilterOnResultExecutedEventData.EventName,
                    new AfterResultFilterOnResultExecutedEventData(
                        resultExecutedContext.ActionDescriptor,
                        resultExecutedContext,
                        filter
                    ));
            }
        }

        public static void BeforeActionResult(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            IActionResult result)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(result != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                BeforeActionResultImpl(diagnosticListener, actionContext, result);
            }
        }

        private static void BeforeActionResultImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, IActionResult result)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.BeforeActionResultEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.BeforeActionResultEventData.EventName,
                    new BeforeActionResultEventData(actionContext, result));
            }
        }

        public static void AfterActionResult(
            this DiagnosticListener diagnosticListener,
            ActionContext actionContext,
            IActionResult result)
        {
            Debug.Assert(diagnosticListener != null);
            Debug.Assert(actionContext != null);
            Debug.Assert(result != null);

            // Inlinable fast-path check if diagnostic listener is enabled
            if (diagnosticListener.IsEnabled())
            {
                AfterActionResultImpl(diagnosticListener, actionContext, result);
            }
        }

        private static void AfterActionResultImpl(DiagnosticListener diagnosticListener, ActionContext actionContext, IActionResult result)
        {
            if (diagnosticListener.IsEnabled(Diagnostics.AfterActionResultEventData.EventName))
            {
                diagnosticListener.Write(
                    Diagnostics.AfterActionResultEventData.EventName,
                    new AfterActionResultEventData(actionContext, result));
            }
        }
    }
}
