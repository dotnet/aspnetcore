// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptor
    {
        public ActionDescriptor()
        {
            RouteValueDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual string Name { get; set; }

        public List<RouteDataActionConstraint> RouteConstraints { get; set; }

        public AttributeRouteInfo AttributeRouteInfo { get; set; }

        public Dictionary<string, object> RouteValueDefaults { get; private set; }

        public List<HttpMethodConstraint> MethodConstraints { get; set; }

        public List<IActionConstraint> DynamicConstraints { get; set; }

        public List<ParameterDescriptor> Parameters { get; set; }

        public List<FilterDescriptor> FilterDescriptors { get; set; }

        /// <summary>
        /// A friendly name for this action.
        /// </summary>
        public virtual string DisplayName { get; set; }
    }
}
