// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Diagnostics;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class MvcRouteHandler : IRouter
    {
        private bool _servicesRetrieved;

        private IActionContextAccessor _actionContextAccessor;
        private IActionInvokerFactory _actionInvokerFactory;
        private IActionSelector _actionSelector;
        private ILogger _logger;
        private DiagnosticSource _diagnosticSource;

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // We return null here because we're not responsible for generating the url, the route is.
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureServices(context.HttpContext);

            var actionDescriptor = _actionSelector.Select(context);
            if (actionDescriptor == null)
            {
                _logger.NoActionsMatched();
                return TaskCache.CompletedTask;
            }

            if (actionDescriptor.RouteValueDefaults != null)
            {
                foreach (var kvp in actionDescriptor.RouteValueDefaults)
                {
                    if (!context.RouteData.Values.ContainsKey(kvp.Key))
                    {
                        context.RouteData.Values.Add(kvp.Key, kvp.Value);
                    }
                }

                // Removing RouteGroup from RouteValues to simulate the result of conventional routing
                context.RouteData.Values.Remove(TreeRouter.RouteGroupKey);
            }

            context.Handler = (c) => InvokeActionAsync(c, actionDescriptor);
            return TaskCache.CompletedTask;
        }

        private async Task InvokeActionAsync(HttpContext httpContext, ActionDescriptor actionDescriptor)
        {
            var routeData = httpContext.GetRouteData();
            try
            {
                _diagnosticSource.BeforeAction(actionDescriptor, httpContext, routeData);

                using (_logger.ActionScope(actionDescriptor))
                {
                    _logger.ExecutingAction(actionDescriptor);

                    var startTimestamp = _logger.IsEnabled(LogLevel.Information) ? Stopwatch.GetTimestamp() : 0;

                    var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
                    if (_actionContextAccessor != null)
                    {
                        _actionContextAccessor.ActionContext = actionContext;
                    }

                    var invoker = _actionInvokerFactory.CreateInvoker(actionContext);
                    if (invoker == null)
                    {
                        throw new InvalidOperationException(
                            Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(
                                actionDescriptor.DisplayName));
                    }

                    await invoker.InvokeAsync();

                    _logger.ExecutedAction(actionDescriptor, startTimestamp);
                }
            }
            finally
            {
                _diagnosticSource.AfterAction(actionDescriptor, httpContext, routeData);
            }
        }

        private void EnsureServices(HttpContext context)
        {
            if (_servicesRetrieved)
            {
                return;
            }

            var services = context.RequestServices;

            // Verify if AddMvc was done before calling UseMvc
            // We use the MvcMarkerService to make sure if all the services were added.
            MvcServicesHelper.ThrowIfMvcNotRegistered(services);

            // The IActionContextAccessor is optional. We want to avoid the overhead of using CallContext
            // if possible.
            _actionContextAccessor = services.GetService<IActionContextAccessor>();

            _actionInvokerFactory = services.GetRequiredService<IActionInvokerFactory>();
            _actionSelector = services.GetRequiredService<IActionSelector>();
            _diagnosticSource = services.GetRequiredService<DiagnosticSource>();

            var factory = services.GetRequiredService<ILoggerFactory>();
            _logger = factory.CreateLogger<MvcRouteHandler>();

            _servicesRetrieved = true;
        }
    }
}
