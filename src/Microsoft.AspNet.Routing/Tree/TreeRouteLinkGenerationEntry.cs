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
        /// The <see cref="TemplateBinder"/>.
        /// </summary>
        public TemplateBinder Binder { get; set; }

        /// <summary>
        /// The route constraints.
        /// </summary>
        public IReadOnlyDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// The route defaults.
        /// </summary>
        public IReadOnlyDictionary<string, object> Defaults { get; set; }

        /// <summary>
        /// The order of the template.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The precedence of the template for link generation. Greater number means higher precedence.
        /// </summary>
        public decimal GenerationPrecedence { get; set; }

        /// <summary>
        /// The name of the route.
        /// </summary>
        public string Name { get; set; }

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
        public RouteTemplate Template { get; set; }
    }
}