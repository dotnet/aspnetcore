// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Used to build an <see cref="AttributeRoute"/>. Represents an individual route that will be aggregated
    /// into the <see cref="AttributeRoute"/>.
    /// </summary>
    public class AttributeRouteEntry
    {
        /// <summary>
        /// The precedence of the template.
        /// </summary>
        public decimal Precedence { get; set; }

        /// <summary>
        /// The <see cref="TemplateRoute"/>.
        /// </summary>
        public TemplateRoute Route { get; set; }
    }
}