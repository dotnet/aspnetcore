// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;

        public DefaultControllerFactory(IServiceProvider serviceProvider, ITypeActivator activator)
        {
            _serviceProvider = serviceProvider;
            _activator = activator;
        }

        public object CreateController(ActionContext actionContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ReflectedActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatDefaultControllerFactory_ActionDescriptorMustBeReflected(
                        typeof(ReflectedActionDescriptor)),
                    "actionContext");
            }

            var controller = _activator.CreateInstance(_serviceProvider, actionDescriptor.ControllerDescriptor.ControllerTypeInfo.AsType());

            InitializeController(controller, actionContext);

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

        private void InitializeController(object controller, ActionContext actionContext)
        {
            Injector.InjectProperty(controller, "ActionContext", actionContext);

            var viewData = new ViewDataDictionary(
                _serviceProvider.GetService<IModelMetadataProvider>(),
                actionContext.ModelState);
            Injector.InjectProperty(controller, "ViewData", viewData);

            var urlHelper = _serviceProvider.GetService<IUrlHelper>();
            Injector.InjectProperty(controller, "Url", urlHelper);

            Injector.CallInitializer(controller, _serviceProvider);
        }
    }
}
