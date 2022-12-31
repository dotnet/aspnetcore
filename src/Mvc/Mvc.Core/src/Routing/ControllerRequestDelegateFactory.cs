// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class ControllerRequestDelegateFactory : IRequestDelegateFactory
{
    private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
    private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
    private readonly int _maxModelValidationErrors;
    private readonly int? _maxValidationDepth;
    private readonly int _maxModelBindingRecursionDepth;
    private readonly ILogger _logger;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly IActionResultTypeMapper _mapper;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly bool _enableActionInvokers;

    public ControllerRequestDelegateFactory(
        ControllerActionInvokerCache controllerActionInvokerCache,
        IOptions<MvcOptions> optionsAccessor,
        ILoggerFactory loggerFactory,
        DiagnosticListener diagnosticListener,
        IActionResultTypeMapper mapper)
        : this(controllerActionInvokerCache, optionsAccessor, loggerFactory, diagnosticListener, mapper, null)
    {
    }

    public ControllerRequestDelegateFactory(
        ControllerActionInvokerCache controllerActionInvokerCache,
        IOptions<MvcOptions> optionsAccessor,
        ILoggerFactory loggerFactory,
        DiagnosticListener diagnosticListener,
        IActionResultTypeMapper mapper,
        IActionContextAccessor? actionContextAccessor)
    {
        _controllerActionInvokerCache = controllerActionInvokerCache;
        _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
        _maxModelValidationErrors = optionsAccessor.Value.MaxModelValidationErrors;
        _maxValidationDepth = optionsAccessor.Value.MaxValidationDepth;
        _maxModelBindingRecursionDepth = optionsAccessor.Value.MaxModelBindingRecursionDepth;
        _enableActionInvokers = optionsAccessor.Value.EnableActionInvokers;
        _logger = loggerFactory.CreateLogger<ControllerActionInvoker>();
        _diagnosticListener = diagnosticListener;
        _mapper = mapper;
        _actionContextAccessor = actionContextAccessor ?? ActionContextAccessor.Null;
    }

    public RequestDelegate? CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary? dataTokens)
    {
        // Fallback to action invoker extensibility so that invokers can override any default behaviors
        if (_enableActionInvokers || actionDescriptor is not ControllerActionDescriptor controller)
        {
            return null;
        }

        return context =>
        {
            RouteData routeData;

            if (dataTokens is null or { Count: 0 })
            {
                routeData = new RouteData(context.Request.RouteValues);
            }
            else
            {
                routeData = new RouteData();
                routeData.PushState(router: null, context.Request.RouteValues, dataTokens);
            }

            var controllerContext = new ControllerContext(context, routeData, controller)
            {
                // PERF: These are rarely going to be changed, so let's go copy-on-write.
                ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories)
            };

            controllerContext.ModelState.MaxAllowedErrors = _maxModelValidationErrors;
            controllerContext.ModelState.MaxValidationDepth = _maxValidationDepth;
            controllerContext.ModelState.MaxStateDepth = _maxModelBindingRecursionDepth;

            var (cacheEntry, filters) = _controllerActionInvokerCache.GetCachedResult(controllerContext);

            var invoker = new ControllerActionInvoker(
                _logger,
                _diagnosticListener,
                _actionContextAccessor,
                _mapper,
                controllerContext,
                cacheEntry,
                filters);

            return invoker.InvokeAsync();
        };
    }
}
