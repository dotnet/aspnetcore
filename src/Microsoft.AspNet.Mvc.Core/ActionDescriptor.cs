// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptor
    {
        public virtual string Name { get; set; }

        public List<RouteDataActionConstraint> RouteConstraints { get; set; }

        /// <summary>
        /// The route template May be null if the action has no attribute routes.
        /// </summary>
        public string RouteTemplate { get; set; }

        public List<HttpMethodConstraint> MethodConstraints { get; set; }

        public List<IActionConstraint> DynamicConstraints { get; set; }

        public List<ParameterDescriptor> Parameters { get; set; }

        public List<FilterDescriptor> FilterDescriptors { get; set; }
    }
}
