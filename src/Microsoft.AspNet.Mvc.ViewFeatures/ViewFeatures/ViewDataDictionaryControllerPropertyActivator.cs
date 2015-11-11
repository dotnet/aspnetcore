// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class ViewDataDictionaryControllerPropertyActivator : IControllerPropertyActivator
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<ControllerContext>[]> _getPropertiesToActivate;

        public ViewDataDictionaryControllerPropertyActivator(IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadataProvider = modelMetadataProvider;

            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        public void Activate(ControllerContext actionContext, object controller)
        {
            var controllerType = controller.GetType();
            var propertiesToActivate = _activateActions.GetOrAdd(
                controllerType,
                _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(controller, actionContext);
            }
        }

        private PropertyActivator<ControllerContext>[] GetPropertiesToActivate(Type type)
        {
            var activators = PropertyActivator<ControllerContext>.GetPropertiesToActivate(
                type,
                typeof(ViewDataDictionaryAttribute),
                p => new PropertyActivator<ControllerContext>(p, GetViewDataDictionary));

            return activators;
        }

        private ViewDataDictionary GetViewDataDictionary(ControllerContext context)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            return new ViewDataDictionary(
                _modelMetadataProvider,
                context.ModelState);
        }
    }
}
