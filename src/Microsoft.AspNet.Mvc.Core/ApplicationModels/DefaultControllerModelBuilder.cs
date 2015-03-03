// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// A default implementation of <see cref="IControllerModelBuilder"/>.
    /// </summary>
    public class DefaultControllerModelBuilder : IControllerModelBuilder
    {
        private readonly IActionModelBuilder _actionModelBuilder;
        private readonly ILogger _logger;
        private readonly AuthorizationOptions _authorizationOptions;

        /// <summary>
        /// Creates a new <see cref="DefaultControllerModelBuilder"/>.
        /// </summary>
        /// <param name="actionModelBuilder">The <see cref="IActionModelBuilder"/> used to create actions.</param>
        public DefaultControllerModelBuilder(
            IActionModelBuilder actionModelBuilder, 
            ILoggerFactory loggerFactory, 
            IOptions<AuthorizationOptions> options)
        {
            _actionModelBuilder = actionModelBuilder;
            _logger = loggerFactory.Create<DefaultControllerModelBuilder>();
            _authorizationOptions = options?.Options ?? new AuthorizationOptions();
        }

        /// <inheritdoc />
        public ControllerModel BuildControllerModel([NotNull] TypeInfo typeInfo)
        {
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

            AddRange(controllerModel.ActionConstraints, attributes.OfType<IActionConstraintMetadata>());
            AddRange(controllerModel.Filters, attributes.OfType<IFilter>());
            AddRange(controllerModel.RouteConstraints, attributes.OfType<IRouteConstraintProvider>());

            var policy = AuthorizationPolicy.Combine(_authorizationOptions, attributes.OfType<AuthorizeAttribute>());
            if (policy != null)
            {
                controllerModel.Filters.Add(new AuthorizeFilter(policy));
            }

            AddRange(
                controllerModel.AttributeRoutes,
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

            // Controllers can implement action filter and result filter interfaces. We add
            // a special delegating filter implementation to the pipeline to handle it.
            //
            // This is needed because filters are instantiated before the controller.
            if (typeof(IAsyncActionFilter).GetTypeInfo().IsAssignableFrom(typeInfo) ||
                typeof(IActionFilter).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                controllerModel.Filters.Add(new ControllerActionFilter());
            }
            if (typeof(IAsyncResultFilter).GetTypeInfo().IsAssignableFrom(typeInfo) ||
                typeof(IResultFilter).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                controllerModel.Filters.Add(new ControllerResultFilter());
            }

            return controllerModel;
        }

        private static void AddRange<T>(IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}
