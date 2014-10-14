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
        private readonly IInputFormattersProvider _inputFormattersProvider;
        private readonly INestedProviderManager<FilterProviderContext> _filterProvider;
        private readonly IControllerActionArgumentBinder _actionInvocationInfoProvider;

        public ControllerActionInvokerProvider(IControllerFactory controllerFactory,
                                              IInputFormattersProvider inputFormattersProvider,
                                              INestedProviderManager<FilterProviderContext> filterProvider,
                                              IControllerActionArgumentBinder actionInvocationInfoProvider)
        {
            _controllerFactory = controllerFactory;
            _inputFormattersProvider = inputFormattersProvider;
            _filterProvider = filterProvider;
            _actionInvocationInfoProvider = actionInvocationInfoProvider;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        public void Invoke(ActionInvokerProviderContext context, Action callNext)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (actionDescriptor != null)
            {
                context.Result = new ControllerActionInvoker(
                                    context.ActionContext,
                                    _filterProvider,
                                    _controllerFactory,
                                    actionDescriptor,
                                    _inputFormattersProvider,
                                    _actionInvocationInfoProvider);
            }

            callNext();
        }
    }
}
