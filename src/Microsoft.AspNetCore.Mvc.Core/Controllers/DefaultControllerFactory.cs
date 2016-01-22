// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// Default implementation for <see cref="IControllerFactory"/>.
    /// </summary>
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IControllerActivator _controllerActivator;
        private readonly IControllerPropertyActivator[] _propertyActivators;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultControllerFactory"/>.
        /// </summary>
        /// <param name="controllerActivator">
        /// <see cref="IControllerActivator"/> used to create controller instances.
        /// </param>
        /// <param name="propertyActivators">
        /// A set of <see cref="IControllerPropertyActivator"/> instances used to initialize controller
        /// properties.
        /// </param>
        public DefaultControllerFactory(
            IControllerActivator controllerActivator,
            IEnumerable<IControllerPropertyActivator> propertyActivators)
        {
            _controllerActivator = controllerActivator;
            _propertyActivators = propertyActivators.ToArray();
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
        public virtual object CreateController(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ActionDescriptor == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ControllerContext.ActionDescriptor),
                    nameof(ControllerContext)));
            }

            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
            var controllerTypeInfo = controllerType.GetTypeInfo();
            if (controllerTypeInfo.IsValueType ||
                controllerTypeInfo.IsInterface ||
                controllerTypeInfo.IsAbstract ||
                (controllerTypeInfo.IsGenericType && controllerTypeInfo.IsGenericTypeDefinition))
            {
                var message = Resources.FormatValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated(
                    controllerType.FullName, 
                    GetType().FullName);
                throw new InvalidOperationException(message);
            }

            var controller = _controllerActivator.Create(context, controllerType);
            foreach (var propertyActivator in _propertyActivators)
            {
                propertyActivator.Activate(context, controller);
            }

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
    }
}
