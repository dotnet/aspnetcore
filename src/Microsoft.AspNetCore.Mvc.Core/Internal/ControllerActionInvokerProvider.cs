// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IControllerFactory _controllerFactory;
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly ParameterBinder _parameterBinder;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly int _maxModelValidationErrors;
        private readonly ILogger _logger;
        private readonly DiagnosticSource _diagnosticSource;

        public ControllerActionInvokerProvider(
            IControllerFactory controllerFactory,
            ControllerActionInvokerCache controllerActionInvokerCache,
            ParameterBinder parameterBinder,
            IModelMetadataProvider modelMetadataProvider,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            _controllerFactory = controllerFactory;
            _controllerActionInvokerCache = controllerActionInvokerCache;
            _parameterBinder = parameterBinder;
            _modelMetadataProvider = modelMetadataProvider;
            _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
            _maxModelValidationErrors = optionsAccessor.Value.MaxModelValidationErrors;
            _logger = loggerFactory.CreateLogger<ControllerActionInvoker>();
            _diagnosticSource = diagnosticSource;
        }

        public int Order
        {
            get { return -1000; }
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionDescriptor = context.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (actionDescriptor != null)
            {
                var controllerContext = new ControllerContext(context.ActionContext);
                // PERF: These are rarely going to be changed, so let's go copy-on-write.
                controllerContext.ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories);
                controllerContext.ModelState.MaxAllowedErrors = _maxModelValidationErrors;

                var cacheState = _controllerActionInvokerCache.GetState(controllerContext);

                var invoker = new ControllerActionInvoker(
                    _controllerFactory,
                    _parameterBinder,
                    _modelMetadataProvider,
                    _logger,
                    _diagnosticSource,
                    controllerContext,
                    cacheState.Filters,
                    cacheState.ActionMethodExecutor);

                context.Result = invoker;
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
