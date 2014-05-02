// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

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
