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

            EnsureServices(context.Context);

            // The contract of this method is to check that the values coming in from the route are valid;
            // that they match an existing action, setting IsBound = true if the values are OK.
            context.IsBound = _actionSelector.HasValidAction(context);

            // We return null here because we're not responsible for generating the url, the route is.
            return null;
        }

        public async Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var services = context.HttpContext.RequestServices;

            // Verify if AddMvc was done before calling UseMvc
            // We use the MvcMarkerService to make sure if all the services were added.
            MvcServicesHelper.ThrowIfMvcNotRegistered(services);
            EnsureServices(context.HttpContext);

            var actionDescriptor = await _actionSelector.SelectAsync(context);
            if (actionDescriptor == null)
            {
                _logger.NoActionsMatched();
                return;
            }

            // Replacing the route data allows any code running here to dirty the route values or data-tokens
            // without affecting something upstream.
            var oldRouteData = context.RouteData;
            var newRouteData = new RouteData(oldRouteData);

            if (actionDescriptor.RouteValueDefaults != null)
            {
                foreach (var kvp in actionDescriptor.RouteValueDefaults)
                {
                    if (!newRouteData.Values.ContainsKey(kvp.Key))
                    {
                        newRouteData.Values.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            // Removing RouteGroup from RouteValues to simulate the result of conventional routing
            newRouteData.Values.Remove(TreeRouter.RouteGroupKey);

            try
            {
                context.RouteData = newRouteData;

                _diagnosticSource.BeforeAction(actionDescriptor, context.HttpContext, context.RouteData);

                using (_logger.ActionScope(actionDescriptor))
                {
                    _logger.ExecutingAction(actionDescriptor);

                    var startTime = Environment.TickCount;
                    await InvokeActionAsync(context, actionDescriptor);
                    context.IsHandled = true;

                    _logger.ExecutedAction(actionDescriptor, startTime);
                }
            }
            finally
            {
                _diagnosticSource.AfterAction(actionDescriptor, context.HttpContext, context.RouteData);

                if (!context.IsHandled)
                {
                    context.RouteData = oldRouteData;
                }
            }
        }

        private Task InvokeActionAsync(RouteContext context, ActionDescriptor actionDescriptor)
        {
            var actionContext = new ActionContext(context.HttpContext, context.RouteData, actionDescriptor);
            _actionContextAccessor.ActionContext = actionContext;

            var invoker = _actionInvokerFactory.CreateInvoker(actionContext);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(
                        actionDescriptor.DisplayName));
            }

            return invoker.InvokeAsync();
        }

        private void EnsureServices(HttpContext context)
        {
            if (_actionContextAccessor == null)
            {
                _actionContextAccessor = context.RequestServices.GetRequiredService<IActionContextAccessor>();
            }

            if (_actionInvokerFactory == null)
            {
                _actionInvokerFactory = context.RequestServices.GetRequiredService<IActionInvokerFactory>();
            }

            if (_actionSelector == null)
            {
                _actionSelector = context.RequestServices.GetRequiredService<IActionSelector>();
            }

            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.CreateLogger<MvcRouteHandler>();
            }
            
            if (_diagnosticSource == null)
            {
                _diagnosticSource = context.RequestServices.GetRequiredService<DiagnosticSource>();
            }
        }
    }
}
