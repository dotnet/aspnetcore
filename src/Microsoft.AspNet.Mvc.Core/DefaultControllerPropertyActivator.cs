// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerPropertyActivator : IControllerPropertyActivator
    {
        private readonly ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<ActionContext>[]> _getPropertiesToActivate;

        public DefaultControllerPropertyActivator()
        {
            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        public void Activate(ActionContext actionContext, object controller)
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

        private PropertyActivator<ActionContext>[] GetPropertiesToActivate(Type type)
        {
            IEnumerable<PropertyActivator<ActionContext>> activators;
            activators = PropertyActivator<ActionContext>.GetPropertiesToActivate(
                type,
                typeof(ActionContextAttribute),
                p => new PropertyActivator<ActionContext>(p, c => c));

            activators = activators.Concat(PropertyActivator<ActionContext>.GetPropertiesToActivate(
                type,
                typeof(ActionBindingContextAttribute),
                p => new PropertyActivator<ActionContext>(p, GetActionBindingContext)));

            return activators.ToArray();
        }

        private static ActionBindingContext GetActionBindingContext(ActionContext context)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var accessor = serviceProvider.GetRequiredService<IScopedInstance<ActionBindingContext>>();
            return accessor.Value;
        }
    }
}
