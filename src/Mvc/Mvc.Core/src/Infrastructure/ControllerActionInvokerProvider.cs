// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ControllerActionInvokerProvider : IActionInvokerProvider
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

    public ControllerActionInvokerProvider(
        ControllerActionInvokerCache controllerActionInvokerCache,
        IOptions<MvcOptions> optionsAccessor,
        ILoggerFactory loggerFactory,
        DiagnosticListener diagnosticListener,
        IActionResultTypeMapper mapper)
        : this(controllerActionInvokerCache, optionsAccessor, loggerFactory, diagnosticListener, mapper, null)
    {
    }

    public ControllerActionInvokerProvider(
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
        _logger = loggerFactory.CreateLogger(typeof(ControllerActionInvoker));
        _diagnosticListener = diagnosticListener;
        _mapper = mapper;
        _actionContextAccessor = actionContextAccessor ?? ActionContextAccessor.Null;
    }

    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(ActionInvokerProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor)
        {
            var controllerContext = new ControllerContext(context.ActionContext)
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

            context.Result = invoker;
        }
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(ActionInvokerProviderContext context)
    {
    }
}
