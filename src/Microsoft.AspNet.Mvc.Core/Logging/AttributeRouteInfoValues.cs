// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of a <see cref="AttributeRouteInfo"/>. Logged as a substructure of
    /// <see cref="ActionDescriptorValues"/>.
    /// </summary>
    public class AttributeRouteInfoValues : LoggerStructureBase
    {
        public AttributeRouteInfoValues(AttributeRouteInfo inner)
        {
            Template = inner?.Template;
            Order = inner?.Order;
            Name = inner?.Name;
        }

        /// <summary>
        /// The route template. See <see cref="AttributeRouteInfo.Template"/>.
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// The order of the route. See <see cref="AttributeRouteInfo.Order"/>.
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// The name of the route. See <see cref="AttributeRouteInfo.Name"/>.
        /// </summary>
        public string Name { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}