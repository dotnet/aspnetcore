// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Default implementation for <see cref="IControllerFactory"/>.
    /// </summary>
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IControllerActivator _controllerActivator;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<ActionContext>[]> _getPropertiesToActivate;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultControllerFactory"/>.
        /// </summary>
        /// <param name="controllerActivator"><see cref="IControllerActivator"/> used to create controller
        /// instances.</param>
        public DefaultControllerFactory(IControllerActivator controllerActivator)
        {
            _controllerActivator = controllerActivator;

            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        /// <summary>
        /// The <see cref="IControllerActivator"/> used to create a controller.
        /// </summary>
        protected IControllerActivator ControllerActivator
        {
            get
            {
                return _controllerActivator;
            }
        }

        /// <inheritdoc />
        public virtual object CreateController([NotNull] ActionContext actionContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                    nameof(actionContext));
            }

            var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
            var controllerTypeInfo = controllerType.GetTypeInfo();
            if (controllerTypeInfo.IsValueType ||
                controllerTypeInfo.IsInterface ||
                controllerTypeInfo.IsAbstract ||
                (controllerTypeInfo.IsGenericType && controllerTypeInfo.IsGenericTypeDefinition))
            {
                var message = Resources.FormatValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated(
                    controllerType.FullName, GetType().FullName);
                throw new InvalidOperationException(message);
            }

            var controller = _controllerActivator.Create(actionContext, controllerType);
            ActivateProperties(controller, actionContext);

            return controller;
        }

        /// <inheritdoc />
        public virtual void ReleaseController(object controller)
        {
            var disposableController = controller as IDisposable;

            if (disposableController != null)
            {
                disposableController.Dispose();
            }
        }

        /// <summary>
        /// Activates the specified controller using the specified action context.
        /// </summary>
        /// <param name="controller">The controller to activate.</param>
        /// <param name="context">The context of the executing action.</param>
        protected virtual void ActivateProperties([NotNull] object controller, [NotNull] ActionContext context)
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

            activators = activators.Concat(PropertyActivator<ActionContext>.GetPropertiesToActivate(
                type,
                typeof(ViewDataDictionaryAttribute),
                p => new PropertyActivator<ActionContext>(p, GetViewDataDictionary)));

            return activators.ToArray();
        }

        private static ActionBindingContext GetActionBindingContext(ActionContext context)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var accessor = serviceProvider.GetRequiredService<IScopedInstance<ActionBindingContext>>();
            return accessor.Value;
        }

        private static ViewDataDictionary GetViewDataDictionary(ActionContext context)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            return new ViewDataDictionary(
                serviceProvider.GetRequiredService<IModelMetadataProvider>(),
                context.ModelState);
        }
    }
}
