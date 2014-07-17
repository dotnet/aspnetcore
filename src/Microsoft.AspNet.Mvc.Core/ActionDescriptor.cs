// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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

        /// <summary>
        /// The set of route values that are added when this action is selected.
        /// </summary>
        public Dictionary<string, object> RouteValueDefaults { get; private set; }

        /// <summary>
        /// The attribute route template. May be null if the action has no attribute routes.
        /// </summary>
        public string AttributeRouteTemplate { get; set; }

        public List<HttpMethodConstraint> MethodConstraints { get; set; }

        public List<IActionConstraint> DynamicConstraints { get; set; }

        public List<ParameterDescriptor> Parameters { get; set; }

        public List<FilterDescriptor> FilterDescriptors { get; set; }
    }
}
