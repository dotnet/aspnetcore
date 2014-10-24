// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// A default implementation of <see cref="IControllerModelBuilder"/>.
    /// </summary>
    public class DefaultControllerModelBuilder : IControllerModelBuilder
    {
        private readonly IActionModelBuilder _actionModelBuilder;

        /// <summary>
        /// Creates a new <see cref="DefaultControllerModelBuilder"/>.
        /// </summary>
        /// <param name="actionModelBuilder">The <see cref="IActionModelBuilder"/> used to create actions.</param>
        public DefaultControllerModelBuilder(IActionModelBuilder actionModelBuilder)
        {
            _actionModelBuilder = actionModelBuilder;
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
                var actionModels = _actionModelBuilder.BuildActionModels(methodInfo);
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
            if (!typeInfo.IsClass ||
                typeInfo.IsAbstract ||

                // We only consider public top-level classes as controllers. IsPublic returns false for nested
                // classes, regardless of visibility modifiers.
                !typeInfo.IsPublic ||
                typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.Name.Equals("Controller", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                   typeof(Controller).GetTypeInfo().IsAssignableFrom(typeInfo);
        }

        /// <summary>
        /// Creates an <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>A <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.</returns>
        protected virtual ControllerModel CreateControllerModel([NotNull] TypeInfo typeInfo)
        {
            var controllerModel = new ControllerModel(typeInfo);

            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToArray() is object
            var attributes = typeInfo.GetCustomAttributes(inherit: true).OfType<object>().ToArray();
            controllerModel.Attributes.AddRange(attributes);

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