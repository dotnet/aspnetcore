// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;
        private readonly IControllerActivator _controllerActivator;

        public DefaultControllerFactory(IServiceProvider serviceProvider, 
                                        ITypeActivator typeActivator,
                                        IControllerActivator controllerActivator)
        {
            _serviceProvider = serviceProvider;
            _typeActivator = typeActivator;
            _controllerActivator = controllerActivator;
        }

        public object CreateController(ActionContext actionContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                    "actionContext");
            }

            var controller = _typeActivator.CreateInstance(
                _serviceProvider,
                actionDescriptor.ControllerDescriptor.ControllerTypeInfo.AsType());

            actionContext.Controller = controller;
            _controllerActivator.Activate(controller, actionContext);

            return controller;
        }

        public void ReleaseController(object controller)
        {
            var disposableController = controller as IDisposable;

            if (disposableController != null)
            {
                disposableController.Dispose();
            }
        }
    }
}
