// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class MvcAttributeRouteHandler : IRouter
{
    private readonly IActionInvokerFactory _actionInvokerFactory;
    private readonly IActionSelector _actionSelector;
    private readonly ILogger _logger;
    private readonly DiagnosticListener _diagnosticListener;

    public MvcAttributeRouteHandler(
        IActionInvokerFactory actionInvokerFactory,
        IActionSelector actionSelector,
        DiagnosticListener diagnosticListener,
        ILoggerFactory loggerFactory)
    {
        _actionInvokerFactory = actionInvokerFactory;
        _actionSelector = actionSelector;
        _diagnosticListener = diagnosticListener;
        _logger = loggerFactory.CreateLogger(typeof(MvcAttributeRouteHandler));
    }

    public ActionDescriptor[]? Actions { get; set; }

    public VirtualPathData? GetVirtualPath(VirtualPathContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // We return null here because we're not responsible for generating the url, the route is.
        return null;
    }

    public Task RouteAsync(RouteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Actions == null)
        {
            var message = Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(Actions),
                nameof(MvcAttributeRouteHandler));
            throw new InvalidOperationException(message);
        }

        var actionDescriptor = _actionSelector.SelectBestCandidate(context, Actions);
        if (actionDescriptor == null)
        {
            _logger.NoActionsMatched(context.RouteData.Values);
            return Task.CompletedTask;
        }

        foreach (var kvp in actionDescriptor.RouteValues)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                context.RouteData.Values[kvp.Key] = kvp.Value;
            }
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
