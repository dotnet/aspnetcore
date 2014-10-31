// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// Information about the current routing path.
    /// </summary>
    public class RouteData
    {
        /// <summary>
        /// Creates a new <see cref="RouteData"/> instance.
        /// </summary>
        public RouteData()
        {
            DataTokens = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Routers = new List<IRouter>();
            Values = new RouteValueDictionary();
        }

        /// <summary>
        /// Creates a new <see cref="RouteData"/> instance with values copied from <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other <see cref="RouteData"/> instance to copy.</param>
        public RouteData([NotNull] RouteData other)
        {
            DataTokens = new Dictionary<string, object>(other.DataTokens, StringComparer.OrdinalIgnoreCase);
            Routers = new List<IRouter>(other.Routers);
            Values = new RouteValueDictionary(other.Values);
        }

        /// <summary>
        /// Gets the data tokens produced by routes on the current routing path.
        /// </summary>
        public IDictionary<string, object> DataTokens { get; private set; }

        /// <summary>
        /// Gets the list of <see cref="IRouter"/> instances on the current routing path.
        /// </summary>
        public List<IRouter> Routers { get; private set; }

        /// <summary>
        /// Gets the set of values produced by routes on the current routing path.
        /// </summary>
        public IDictionary<string, object> Values { get; private set; }
    }
}