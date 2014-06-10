// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            Filters = Attributes.OfType<IFilter>().ToList();
            RouteConstraints = Attributes.OfType<RouteConstraintAttribute>().ToList();

            var routeTemplateAttribute = Attributes.OfType<IRouteTemplateProvider>().FirstOrDefault();
            if (routeTemplateAttribute != null)
            {
                RouteTemplate = routeTemplateAttribute.Template;
            }

            ControllerName = controllerType.Name.EndsWith("Controller", StringComparison.Ordinal)
                        ? controllerType.Name.Substring(0, controllerType.Name.Length - "Controller".Length)
                        : controllerType.Name;
        }

        public List<ReflectedActionModel> Actions { get; private set; }

        public List<object> Attributes { get; private set; }

        public string ControllerName { get; set; }

        public TypeInfo ControllerType { get; private set; }

        public List<IFilter> Filters { get; private set; }

        public List<RouteConstraintAttribute> RouteConstraints { get; private set; }

        public string RouteTemplate { get; set; }
    }
}