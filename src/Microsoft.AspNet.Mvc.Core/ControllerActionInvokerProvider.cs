// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IControllerActionArgumentBinder _argumentBinder;
        private readonly IControllerFactory _controllerFactory;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IInputFormattersProvider _inputFormattersProvider;
        private readonly IModelBinderProvider _modelBinderProvider;
        private readonly IModelValidatorProviderProvider _modelValidationProviderProvider;
        private readonly IValueProviderFactoryProvider _valueProviderFactoryProvider;
        private readonly IScopedInstance<ActionBindingContext> _actionBindingContextAccessor;
        private readonly ITempDataDictionary _tempData;

        public ControllerActionInvokerProvider(
            IControllerFactory controllerFactory,
            IInputFormattersProvider inputFormattersProvider,
            IEnumerable<IFilterProvider> filterProviders,
            IControllerActionArgumentBinder argumentBinder,
            IModelBinderProvider modelBinderProvider,
            IModelValidatorProviderProvider modelValidationProviderProvider,
            IValueProviderFactoryProvider valueProviderFactoryProvider,
            IScopedInstance<ActionBindingContext> actionBindingContextAccessor,
            ITempDataDictionary tempData)
        {
            _controllerFactory = controllerFactory;
            _inputFormattersProvider = inputFormattersProvider;
            _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
            _argumentBinder = argumentBinder;
            _modelBinderProvider = modelBinderProvider;
            _modelValidationProviderProvider = modelValidationProviderProvider;
            _valueProviderFactoryProvider = valueProviderFactoryProvider;
            _actionBindingContextAccessor = actionBindingContextAccessor;
            _tempData = tempData;
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
                                    _inputFormattersProvider,
                                    _argumentBinder,
                                    _modelBinderProvider,
                                    _modelValidationProviderProvider,
                                    _valueProviderFactoryProvider,
                                    _actionBindingContextAccessor,
                                    _tempData);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted([NotNull] ActionInvokerProviderContext context)
        {
        }
    }
}
