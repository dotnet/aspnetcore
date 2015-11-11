// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Mvc.Controllers
{
    public class ControllerActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IControllerActionArgumentBinder _argumentBinder;
        private readonly IControllerFactory _controllerFactory;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IReadOnlyList<IInputFormatter> _inputFormatters;
        private readonly IReadOnlyList<IModelBinder> _modelBinders;
        private readonly IReadOnlyList<IModelValidatorProvider> _modelValidatorProviders;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly int _maxModelValidationErrors;
        private readonly ILogger _logger;
        private readonly DiagnosticSource _diagnosticSource;

        public ControllerActionInvokerProvider(
            IControllerFactory controllerFactory,
            IEnumerable<IFilterProvider> filterProviders,
            IControllerActionArgumentBinder argumentBinder,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            _controllerFactory = controllerFactory;
            _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
            _argumentBinder = argumentBinder;
            _inputFormatters = optionsAccessor.Value.InputFormatters.ToArray();
            _modelBinders = optionsAccessor.Value.ModelBinders.ToArray();
            _modelValidatorProviders = optionsAccessor.Value.ModelValidatorProviders.ToArray();
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
                    context.ActionContext,
                    _filterProviders,
                    _controllerFactory,
                    actionDescriptor,
                    _inputFormatters,
                    _argumentBinder,
                    _modelBinders,
                    _modelValidatorProviders,
                    _valueProviderFactories,
                    _logger,
                    _diagnosticSource,
                    _maxModelValidationErrors);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {
        }
    }
}
