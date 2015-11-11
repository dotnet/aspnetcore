// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Mvc.Controllers
{
    public class DefaultControllerPropertyActivator : IControllerPropertyActivator
    {
        private readonly ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<ControllerContext>[]> _getPropertiesToActivate;

        public DefaultControllerPropertyActivator()
        {
            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        public void Activate(ControllerContext context, object controller)
        {
            var controllerType = controller.GetType();
            var propertiesToActivate = _activateActions.GetOrAdd(
                controllerType,
                _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(controller, context);
            }
        }

        private PropertyActivator<ControllerContext>[] GetPropertiesToActivate(Type type)
        {
            IEnumerable<PropertyActivator<ControllerContext>> activators;
            activators = PropertyActivator<ControllerContext>.GetPropertiesToActivate(
                type,
                typeof(ActionContextAttribute),
                p => new PropertyActivator<ControllerContext>(p, c => c));

            activators = activators.Concat(PropertyActivator<ControllerContext>.GetPropertiesToActivate(
                type,
                typeof(ControllerContextAttribute),
                p => new PropertyActivator<ControllerContext>(p, c => c)));

            return activators.ToArray();
        }
    }
}
