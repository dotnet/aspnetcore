// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Routing.Tree
{
    /// <summary>
    /// Used to build a <see cref="TreeRouter"/>. Represents an individual URL-generating route that will be
    /// aggregated into the <see cref="TreeRouter"/>.
    /// </summary>
    public class TreeRouteLinkGenerationEntry
    {
        /// <summary>
        /// Gets or sets the <see cref="TemplateBinder"/>.
        /// </summary>
        public TemplateBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the route constraints.
        /// </summary>
        public IDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the route defaults.
        /// </summary>
        public IDictionary<string, object> Defaults { get; set; }

        /// <summary>
        /// Gets or sets the order of the template.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the precedence of the template for link generation. A greater value of
        /// <see cref="GenerationPrecedence"/> means that an entry is considered first.
        /// </summary>
        public decimal GenerationPrecedence { get; set; }

        /// <summary>
        /// Gets or sets the name of the route.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the route group.
        /// </summary>
        public string RouteGroup { get; set; }

        /// <summary>
        /// Gets or sets the set of values that must be present for link genration.
        /// </summary>
        public IDictionary<string, object> RequiredLinkValues { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Template"/>.
        /// </summary>
        public RouteTemplate Template { get; set; }
    }
}