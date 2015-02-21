// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Http;
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
        private readonly IDictionary<Type, Func<ActionContext, object>> _valueAccessorLookup;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]> _activateActions;
        private readonly Func<Type, PropertyActivator<ActionContext>[]> _getPropertiesToActivate;
        private readonly Func<Type, Func<ActionContext, object>> _getRequiredService = GetRequiredService;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultControllerFactory"/>.
        /// </summary>
        /// <param name="controllerActivator"><see cref="IControllerActivator"/> used to create controller
        /// instances.</param>
        public DefaultControllerFactory(IControllerActivator controllerActivator)
        {
            _controllerActivator = controllerActivator;
            _valueAccessorLookup = CreateValueAccessorLookup();
            _activateActions = new ConcurrentDictionary<Type, PropertyActivator<ActionContext>[]>();
            _getPropertiesToActivate = GetPropertiesToActivate;
        }

        /// <inheritdoc />
        public object CreateController([NotNull] ActionContext actionContext)
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
            var controller = _controllerActivator.Create(actionContext, controllerType);
            ActivateProperties(controller, actionContext);

            return controller;
        }

        /// <inheritdoc />
        public void ReleaseController(object controller)
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
            var controllerTypeInfo = controllerType.GetTypeInfo();
            if (controllerTypeInfo.IsValueType)
            {
                var message = Resources.FormatValueTypesCannotBeActivated(GetType().FullName);
                throw new InvalidOperationException(message);
            }

            var propertiesToActivate = _activateActions.GetOrAdd(controllerType,
                                                                 _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(controller, context);
            }
        }

        /// <summary>
        /// Returns a <see cref="IDictionary{TKey, TValue}"/> of property types to delegates used to activate
        /// controller properties annotated with <see cref="ActivateAttribute"/>.
        /// </summary>
        /// <returns>A dictionary containing the property type to activator delegate mapping.</returns>
        /// <remarks>Override this method to provide custom activation behavior for controller properties
        /// annotated with <see cref="ActivateAttribute"/>.</remarks>
        protected virtual IDictionary<Type, Func<ActionContext, object>> CreateValueAccessorLookup()
        {
            var dictionary = new Dictionary<Type, Func<ActionContext, object>>
            {
                { typeof(ActionContext), (context) => context },
                { typeof(HttpContext), (context) => context.HttpContext },
                { typeof(HttpRequest), (context) => context.HttpContext.Request },
                { typeof(HttpResponse), (context) => context.HttpContext.Response },
                {
                    typeof(ViewDataDictionary),
                    (context) =>
                    {
                        var serviceProvider = context.HttpContext.RequestServices;
                        return new ViewDataDictionary(
                            serviceProvider.GetRequiredService<IModelMetadataProvider>(),
                            context.ModelState);
                    }
                },
                {
                    typeof(ActionBindingContext),
                    (context) =>
                    {
                        var serviceProvider = context.HttpContext.RequestServices;
                        var accessor = serviceProvider.GetRequiredService<IScopedInstance<ActionBindingContext>>();
                        return accessor.Value;
                    }
                }
            };

            return dictionary;
        }

        private PropertyActivator<ActionContext>[] GetPropertiesToActivate(Type type)
        {
            var activatorsForActivateProperties = PropertyActivator<ActionContext>.GetPropertiesToActivate(
                                                        type,
                                                        typeof(ActivateAttribute),
                                                        CreateActivateInfo);
            var activatorsForFromServiceProperties = PropertyActivator<ActionContext>.GetPropertiesToActivate(
                                                        type,
                                                        typeof(FromServicesAttribute),
                                                        CreateFromServicesInfo);

            return Enumerable.Concat(activatorsForActivateProperties, activatorsForFromServiceProperties)
                             .ToArray();
        }

        private PropertyActivator<ActionContext> CreateActivateInfo(
            PropertyInfo property)
        {
            Func<ActionContext, object> valueAccessor;
            if (!_valueAccessorLookup.TryGetValue(property.PropertyType, out valueAccessor))
            {
                var message = Resources.FormatControllerFactory_PropertyCannotBeActivated(
                                                    property.Name,
                                                    property.DeclaringType.FullName);
                throw new InvalidOperationException(message);
            }

            return new PropertyActivator<ActionContext>(property, valueAccessor);
        }

        private PropertyActivator<ActionContext> CreateFromServicesInfo(
            PropertyInfo property)
        {
            var valueAccessor = _getRequiredService(property.PropertyType);
            return new PropertyActivator<ActionContext>(property, valueAccessor);
        }

        private static Func<ActionContext, object> GetRequiredService(Type propertyType)
        {
            return actionContext => actionContext.HttpContext.RequestServices.GetRequiredService(propertyType);
        }
    }
}
