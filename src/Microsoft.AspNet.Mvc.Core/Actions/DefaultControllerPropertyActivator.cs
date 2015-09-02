// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Actions
{
    public class DefaultControllerPropertyActivator : IControllerPropertyActivator
    {
        private readonly IActionBindingContextAccessor _actionBindingContextAccessor;
        private readonly ConcurrentDictionary<Type, PropertyActivator<Contexts>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<Contexts>[]> _getPropertiesToActivate;

        public DefaultControllerPropertyActivator(IActionBindingContextAccessor actionBindingContextAccessor)
        {
            _actionBindingContextAccessor = actionBindingContextAccessor;

            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<Contexts>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        public void Activate(ActionContext actionContext, object controller)
        {
            var controllerType = controller.GetType();
            var propertiesToActivate = _activateActions.GetOrAdd(
                controllerType,
                _getPropertiesToActivate);

            var contexts = new Contexts()
            {
                ActionBindingContext = _actionBindingContextAccessor.ActionBindingContext,
                ActionContext = actionContext,
            };

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(controller, contexts);
            }
        }

        private PropertyActivator<Contexts>[] GetPropertiesToActivate(Type type)
        {
            IEnumerable<PropertyActivator<Contexts>> activators;
            activators = PropertyActivator<Contexts>.GetPropertiesToActivate(
                type,
                typeof(ActionContextAttribute),
                p => new PropertyActivator<Contexts>(p, c => c.ActionContext));

            activators = activators.Concat(PropertyActivator<Contexts>.GetPropertiesToActivate(
                type,
                typeof(ActionBindingContextAttribute),
                p => new PropertyActivator<Contexts>(p, c => c.ActionBindingContext)));

            return activators.ToArray();
        }

        private struct Contexts
        {
            public ActionBindingContext ActionBindingContext;
            public ActionContext ActionContext;
        }
    }
}
