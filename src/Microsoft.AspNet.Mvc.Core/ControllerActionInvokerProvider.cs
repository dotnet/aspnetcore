// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IControllerActionArgumentBinder _argumentBinder;
        private readonly IControllerFactory _controllerFactory;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IReadOnlyList<IInputFormatter> _inputFormatters;
        private readonly IReadOnlyList<IModelBinder> _modelBinders;
        private readonly IReadOnlyList<IOutputFormatter> _outputFormatters;
        private readonly IReadOnlyList<IModelValidatorProvider> _modelValidatorProviders;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly IScopedInstance<ActionBindingContext> _actionBindingContextAccessor;
        private readonly ITempDataDictionary _tempData;
        private readonly int _maxModelValidationErrors;
        private readonly ILoggerFactory _loggerFactory;

        public ControllerActionInvokerProvider(
            IControllerFactory controllerFactory,
            IEnumerable<IFilterProvider> filterProviders,
            IControllerActionArgumentBinder argumentBinder,
            IOptions<MvcOptions> optionsAccessor,
            IScopedInstance<ActionBindingContext> actionBindingContextAccessor,
            ITempDataDictionary tempData,
            ILoggerFactory loggerFactory)
        {
            _controllerFactory = controllerFactory;
            _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
            _argumentBinder = argumentBinder;
            _inputFormatters = optionsAccessor.Options.InputFormatters.ToArray();
            _outputFormatters = optionsAccessor.Options.OutputFormatters.ToArray();
            _modelBinders = optionsAccessor.Options.ModelBinders.ToArray();
            _modelValidatorProviders = optionsAccessor.Options.ModelValidatorProviders.ToArray();
            _valueProviderFactories = optionsAccessor.Options.ValueProviderFactories.ToArray();
            _actionBindingContextAccessor = actionBindingContextAccessor;
            _tempData = tempData;
            _maxModelValidationErrors = optionsAccessor.Options.MaxModelValidationErrors;
            _loggerFactory = loggerFactory;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        /// <inheritdoc />
        public void OnProvidersExecuting([NotNull] ActionInvokerProviderContext context)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (actionDescriptor != null)
            {
                context.Result = new ControllerActionInvoker(
                                    context.ActionContext,
                                    _filterProviders,
                                    _controllerFactory,
                                    actionDescriptor,
                                    _inputFormatters,
                                    _outputFormatters,
                                    _argumentBinder,
                                    _modelBinders,
                                    _modelValidatorProviders,
                                    _valueProviderFactories,
                                    _actionBindingContextAccessor,
                                    _tempData,
                                    _loggerFactory,
                                    _maxModelValidationErrors);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted([NotNull] ActionInvokerProviderContext context)
        {
        }
    }
}
