// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class MvcRouteHandler : IRouter
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

    public VirtualPathData? GetVirtualPath(VirtualPathContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // We return null here because we're not responsible for generating the url, the route is.
        return null;
    }

    public Task RouteAsync(RouteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
