// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    public class ControllerModel
    {
        public ControllerModel([NotNull] TypeInfo controllerType)
        {
            ControllerType = controllerType;

            Actions = new List<ActionModel>();
            Attributes = new List<object>();
            AttributeRoutes = new List<AttributeRouteModel>();
            ActionConstraints = new List<IActionConstraintMetadata>();
            Filters = new List<IFilter>();
            RouteConstraints = new List<RouteConstraintAttribute>();
        }

        public List<IActionConstraintMetadata> ActionConstraints { get; private set; }

        public List<ActionModel> Actions { get; private set; }

        public GlobalModel Application { get; set; }

        public List<object> Attributes { get; private set; }

        public string ControllerName { get; set; }

        public TypeInfo ControllerType { get; private set; }

        public List<IFilter> Filters { get; private set; }

        public List<RouteConstraintAttribute> RouteConstraints { get; private set; }

        public List<AttributeRouteModel> AttributeRoutes { get; private set; }

        /// <summary>
        /// If <c>true</c>, <see cref="Description.ApiDescription"/> objects will be created for actions defined by
        /// this controller. 
        /// </summary>
        public bool? ApiExplorerIsVisible { get; set; }

        /// <summary>
        /// The value for <see cref="Description.ApiDescription.GroupName"/> of 
        /// <see cref="Description.ApiDescription"/> objects created for actions defined by this controller.
        /// </summary>
        public string ApiExplorerGroupName { get; set; }
    }
}
