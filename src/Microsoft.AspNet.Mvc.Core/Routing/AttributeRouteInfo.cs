// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Represents the routing information for an action that is attribute routed.
    /// </summary>
    public class AttributeRouteInfo
    {
        /// <summary>
        /// The route template. May be null if the action has no attribute routes.
        /// </summary>
        public string Template { get; set; }
    }
}