// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class MvcRouteHandler : IRouter
    {
        private readonly IActionInvokerFactory _actionInvokerFactory;
        private readonly IActionSelector _actionSelector;
        private readonly ILogger _logger;
        private readonly DiagnosticListener _diagnosticListener;

        public MvcRouteHandler(
            IActionInvokerFactory actionInvokerFactory,
            IActionSelector actionSelector,
            DiagnosticListener diagnosticListener,
            ILoggerFactory loggerFactory)
        {
            _actionInvokerFactory = actionInvokerFactory;
            _actionSelector = actionSelector;
            _diagnosticListener = diagnosticListener;
            _logger = loggerFactory.CreateLogger<MvcRouteHandler>();
        }

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

            var candidates = _actionSelector.SelectCandidates(context);
            if (candidates == null || candidates.Count == 0)
            {
                _logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            var actionDescriptor = _actionSelector.SelectBestCandidate(context, candidates);
            if (actionDescriptor == null)
            {
                _logger.NoActionsMatched(context.RouteData.Values);
                return Task.CompletedTask;
            }

            context.Handler = (c) =>
            {
                var routeData = c.GetRouteData();

                var actionContext = new ActionContext(context.HttpContext, routeData, actionDescriptor);
                var invoker = _actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatActionInvokerFactory_CouldNotCreateInvoker(
                            actionDescriptor.DisplayName));
                }

                return invoker.InvokeAsync();
            };

            return Task.CompletedTask;
        }
    }
}
