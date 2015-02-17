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
            Properties = new Dictionary<object, object>();
            RouteValueDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual string Name { get; set; }

        public IList<RouteDataActionConstraint> RouteConstraints { get; set; }

        public AttributeRouteInfo AttributeRouteInfo { get; set; }

        public IDictionary<string, object> RouteValueDefaults { get; }

        /// <summary>
        /// The set of constraints for this action. Must all be satisfied for the action to be selected.
        /// </summary>
        public IList<IActionConstraintMetadata> ActionConstraints { get; set; }

        public IList<ParameterDescriptor> Parameters { get; set; }

        public IList<FilterDescriptor> FilterDescriptors { get; set; }

        /// <summary>
        /// A friendly name for this action.
        /// </summary>
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="ActionDescriptor"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; }
    }
}
