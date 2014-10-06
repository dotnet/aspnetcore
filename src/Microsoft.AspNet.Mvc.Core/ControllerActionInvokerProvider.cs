// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IControllerFactory _controllerFactory;
        private readonly IActionBindingContextProvider _bindingProvider;
        private readonly IInputFormattersProvider _inputFormattersProvider;
        private readonly INestedProviderManager<FilterProviderContext> _filterProvider;
        private readonly IBodyModelValidator _modelValidator;

        public ControllerActionInvokerProvider(IControllerFactory controllerFactory,
                                              IActionBindingContextProvider bindingProvider,
                                              IInputFormattersProvider inputFormattersProvider,
                                              INestedProviderManager<FilterProviderContext> filterProvider,
                                              IBodyModelValidator modelValidator)
        {
            _controllerFactory = controllerFactory;
            _bindingProvider = bindingProvider;
            _inputFormattersProvider = inputFormattersProvider;
            _filterProvider = filterProvider;
            _modelValidator = modelValidator;
        }

        public int Order
        {
            get { return 0; }
        }

        public void Invoke(ActionInvokerProviderContext context, Action callNext)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (actionDescriptor != null)
            {
                context.Result = new ControllerActionInvoker(
                                    context.ActionContext,
                                    _bindingProvider,
                                    _filterProvider,
                                    _controllerFactory,
                                    actionDescriptor,
                                    _inputFormattersProvider,
                                    _modelValidator);
            }

            callNext();
        }
    }
}
