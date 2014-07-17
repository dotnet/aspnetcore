// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Used to build an <see cref="AttributeRoute"/>. Represents an individual URL-generating route that will be
    /// aggregated into the <see cref="AttributeRoute"/>.
    /// </summary>
    public class AttributeRouteLinkGenerationEntry
    {
        /// <summary>
        /// The <see cref="TemplateBinder"/>.
        /// </summary>
        public TemplateBinder Binder { get; set; }

        /// <summary>
        /// The route constraints.
        /// </summary>
        public IDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// The route defaults.
        /// </summary>
        public IDictionary<string, object> Defaults { get; set; }

        /// <summary>
        /// The precedence of the template.
        /// </summary>
        public decimal Precedence { get; set; }

        /// <summary>
        /// The route group.
        /// </summary>
        public string RouteGroup { get; set; }

        /// <summary>
        /// The set of values that must be present for link genration.
        /// </summary>
        public IDictionary<string, object> RequiredLinkValues { get; set; }

        /// <summary>
        /// The <see cref="Template"/>.
        /// </summary>
        public Template Template { get; set; }
    }
}