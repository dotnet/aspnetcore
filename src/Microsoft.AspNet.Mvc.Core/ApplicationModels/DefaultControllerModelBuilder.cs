// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// A default implementation of <see cref="IControllerModelBuilder"/>.
    /// </summary>
    public class DefaultControllerModelBuilder : IControllerModelBuilder
    {
        private readonly IActionModelBuilder _actionModelBuilder;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="DefaultControllerModelBuilder"/>.
        /// </summary>
        /// <param name="actionModelBuilder">The <see cref="IActionModelBuilder"/> used to create actions.</param>
        public DefaultControllerModelBuilder(IActionModelBuilder actionModelBuilder, ILoggerFactory loggerFactory)
        {
            _actionModelBuilder = actionModelBuilder;
            _logger = loggerFactory.Create<DefaultControllerModelBuilder>();
        }

        /// <inheritdoc />
        public ControllerModel BuildControllerModel([NotNull] TypeInfo typeInfo)
        {
            if (!IsController(typeInfo))
            {
                return null;
            }

            var controllerModel = CreateControllerModel(typeInfo);

            foreach (var methodInfo in typeInfo.AsType().GetMethods())
            {
                var actionModels = _actionModelBuilder.BuildActionModels(typeInfo, methodInfo);
                if (actionModels != null)
                {
                    foreach (var actionModel in actionModels)
                    {
                        actionModel.Controller = controllerModel;
                        controllerModel.Actions.Add(actionModel);
                    }
                }
            }

            return controllerModel;
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns><c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Override this method to provide custom logic to determine which types are considered controllers.
        /// </remarks>
        protected virtual bool IsController([NotNull] TypeInfo typeInfo)
        {
            var status = ControllerStatus.IsController;

            if (!typeInfo.IsClass)
            {
                status |= ControllerStatus.IsNotAClass;
            }
            if (typeInfo.IsAbstract)
            {
                status |= ControllerStatus.IsAbstract;
            }
            // We only consider public top-level classes as controllers. IsPublic returns false for nested
            // classes, regardless of visibility modifiers
            if (!typeInfo.IsPublic)
            {
                status |= ControllerStatus.IsNotPublicOrTopLevel;
            }
            if (typeInfo.ContainsGenericParameters)
            {
                status |= ControllerStatus.ContainsGenericParameters;
            }
            if (typeInfo.Name.Equals("Controller", StringComparison.OrdinalIgnoreCase))
            {
                status |= ControllerStatus.NameIsController;
            }
            if (!typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) &&
                   !typeof(Controller).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                status |= ControllerStatus.DoesNotEndWithControllerAndIsNotAssignable;
            }
            if (_logger.IsEnabled(LogLevel.Verbose))
            {
                _logger.WriteVerbose(new IsControllerValues(
                    typeInfo.AsType(),
                    status));
            }
            return status == ControllerStatus.IsController;
        }

        /// <summary>
        /// Creates an <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>A <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.</returns>
        protected virtual ControllerModel CreateControllerModel([NotNull] TypeInfo typeInfo)
        {
            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToArray() is object
            var attributes = typeInfo.GetCustomAttributes(inherit: true).OfType<object>().ToArray();
            var controllerModel = new ControllerModel(typeInfo, attributes);

            controllerModel.ControllerName =
                typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ?
                    typeInfo.Name.Substring(0, typeInfo.Name.Length - "Controller".Length) :
                    typeInfo.Name;

            controllerModel.ActionConstraints.AddRange(attributes.OfType<IActionConstraintMetadata>());
            controllerModel.Filters.AddRange(attributes.OfType<IFilter>());
            controllerModel.RouteConstraints.AddRange(attributes.OfType<RouteConstraintAttribute>());

            controllerModel.AttributeRoutes.AddRange(
                attributes.OfType<IRouteTemplateProvider>().Select(rtp => new AttributeRouteModel(rtp)));

            var apiVisibility = attributes.OfType<IApiDescriptionVisibilityProvider>().FirstOrDefault();
            if (apiVisibility != null)
            {
                controllerModel.ApiExplorer.IsVisible = !apiVisibility.IgnoreApi;
            }

            var apiGroupName = attributes.OfType<IApiDescriptionGroupNameProvider>().FirstOrDefault();
            if (apiGroupName != null)
            {
                controllerModel.ApiExplorer.GroupName = apiGroupName.GroupName;
            }

            return controllerModel;
        }
    }
}