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
        private readonly IControllerArgumentBinder _argumentBinder;
        private readonly IControllerFactory _controllerFactory;
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly int _maxModelValidationErrors;
        private readonly ILogger _logger;
        private readonly DiagnosticSource _diagnosticSource;

        public ControllerActionInvokerProvider(
            IControllerFactory controllerFactory,
            ControllerActionInvokerCache controllerActionInvokerCache,
            IControllerArgumentBinder argumentBinder,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            _controllerFactory = controllerFactory;
            _controllerActionInvokerCache = controllerActionInvokerCache;
            _argumentBinder = argumentBinder;
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
                context.Result = new ControllerActionInvoker(
                    _controllerActionInvokerCache,
                    _controllerFactory,
                    _argumentBinder,
                    _logger,
                    _diagnosticSource,
                    context.ActionContext,
                    _valueProviderFactories,
                    _maxModelValidationErrors);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
