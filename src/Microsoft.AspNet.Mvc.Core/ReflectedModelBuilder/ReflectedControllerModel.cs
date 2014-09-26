// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    public class ReflectedControllerModel
    {
        public ReflectedControllerModel([NotNull] TypeInfo controllerType)
        {
            ControllerType = controllerType;

            Actions = new List<ReflectedActionModel>();

            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToList() is List<object>
            Attributes = ControllerType.GetCustomAttributes(inherit: true).OfType<object>().ToList();

            Filters = Attributes
                .OfType<IFilter>()
                .ToList();

            RouteConstraints = Attributes.OfType<RouteConstraintAttribute>().ToList();

            AttributeRoutes = Attributes.OfType<IRouteTemplateProvider>()
                .Select(rtp => new ReflectedAttributeRouteModel(rtp))
                .ToList();

            var apiExplorerNameAttribute = Attributes.OfType<IApiDescriptionGroupNameProvider>().FirstOrDefault();
            if (apiExplorerNameAttribute != null)
            {
                ApiExplorerGroupName = apiExplorerNameAttribute.GroupName;
            }

            var apiExplorerVisibilityAttribute = Attributes.OfType<IApiDescriptionVisibilityProvider>().FirstOrDefault();
            if (apiExplorerVisibilityAttribute != null)
            {
                ApiExplorerIsVisible = !apiExplorerVisibilityAttribute.IgnoreApi;
            }

            ControllerName = controllerType.Name.EndsWith("Controller", StringComparison.Ordinal)
                        ? controllerType.Name.Substring(0, controllerType.Name.Length - "Controller".Length)
                        : controllerType.Name;
        }

        public List<ReflectedActionModel> Actions { get; private set; }

        public ReflectedApplicationModel Application { get; set; }

        public List<object> Attributes { get; private set; }

        public string ControllerName { get; set; }

        public TypeInfo ControllerType { get; private set; }

        public List<IFilter> Filters { get; private set; }

        public List<RouteConstraintAttribute> RouteConstraints { get; private set; }

        public List<ReflectedAttributeRouteModel> AttributeRoutes { get; private set; }

        /// <summary>
        /// If <c>true</c>, <see cref="ApiDescription"/> objects will be created for actions defined by this 
        /// controller. 
        /// </summary>
        public bool? ApiExplorerIsVisible { get; set; }

        /// <summary>
        /// The value for <see cref="ApiDescription.GroupName"/> of <see cref="ApiDescription"/> objects created
        /// for actions defined by this controller.
        /// </summary>
        public string ApiExplorerGroupName { get; set; }
    }
}
