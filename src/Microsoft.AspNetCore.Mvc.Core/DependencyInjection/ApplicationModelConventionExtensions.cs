// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains the extension methods for <see cref="AspNetCore.Mvc.MvcOptions.Conventions"/>.
    /// </summary>
    public static class ApplicationModelConventionExtensions
    {
        /// <summary>
        /// Adds a <see cref="IControllerModelConvention"/> to all the controllers in the application.
        /// </summary>
        /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
        /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
        /// <param name="controllerModelConvention">The <see cref="IControllerModelConvention"/> which needs to be
        /// added.</param>
        public static void Add(
            this IList<IApplicationModelConvention> conventions,
            IControllerModelConvention controllerModelConvention)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (controllerModelConvention == null)
            {
                throw new ArgumentNullException(nameof(controllerModelConvention));
            }

            conventions.Add(new ControllerApplicationModelConvention(controllerModelConvention));
        }

        /// <summary>
        /// Adds a <see cref="IActionModelConvention"/> to all the actions in the application.
        /// </summary>
        /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
        /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
        /// <param name="actionModelConvention">The <see cref="IActionModelConvention"/> which needs to be
        /// added.</param>
        public static void Add(
            this IList<IApplicationModelConvention> conventions,
            IActionModelConvention actionModelConvention)
        {
            conventions.Add(new ActionApplicationModelConvention(actionModelConvention));
        }

        private class ActionApplicationModelConvention : IApplicationModelConvention
        {
            private IActionModelConvention _actionModelConvention;

            /// <summary>
            /// Initializes a new instance of <see cref="ActionApplicationModelConvention"/>.
            /// </summary>
            /// <param name="actionModelConvention">The action convention to be applied on all actions
            /// in the application.</param>
            public ActionApplicationModelConvention(IActionModelConvention actionModelConvention)
            {
                if (actionModelConvention == null)
                {
                    throw new ArgumentNullException(nameof(actionModelConvention));
                }

                _actionModelConvention = actionModelConvention;
            }

            /// <inheritdoc />
            public void Apply(ApplicationModel application)
            {
                if (application == null)
                {
                    throw new ArgumentNullException(nameof(application));
                }

                foreach (var controller in application.Controllers)
                {
                    foreach (var action in controller.Actions)
                    {
                        _actionModelConvention.Apply(action);
                    }
                }
            }
        }

        private class ControllerApplicationModelConvention : IApplicationModelConvention
        {
            private IControllerModelConvention _controllerModelConvention;

            /// <summary>
            /// Initializes a new instance of <see cref="ControllerApplicationModelConvention"/>.
            /// </summary>
            /// <param name="controllerConvention">The controller convention to be applied on all controllers
            /// in the application.</param>
            public ControllerApplicationModelConvention(IControllerModelConvention controllerConvention)
            {
                if (controllerConvention == null)
                {
                    throw new ArgumentNullException(nameof(controllerConvention));
                }

                _controllerModelConvention = controllerConvention;
            }

            /// <inheritdoc />
            public void Apply(ApplicationModel application)
            {
                if (application == null)
                {
                    throw new ArgumentNullException(nameof(application));
                }

                foreach (var controller in application.Controllers)
                {
                    _controllerModelConvention.Apply(controller);
                }
            }
        }
    }
}