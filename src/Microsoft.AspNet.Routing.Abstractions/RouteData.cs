// Copyright (c) .NET Foundation. All rights reserved.
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
        private RouteValueDictionary _dataTokens;
        private RouteValueDictionary _values;

        /// <summary>
        /// Creates a new <see cref="RouteData"/> instance.
        /// </summary>
        public RouteData()
        {
            // Perf: Avoid allocating DataTokens and RouteValues unless needed.
            Routers = new List<IRouter>();
        }

        /// <summary>
        /// Creates a new <see cref="RouteData"/> instance with values copied from <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other <see cref="RouteData"/> instance to copy.</param>
        public RouteData(RouteData other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Routers = new List<IRouter>(other.Routers);
            
            // Perf: Avoid allocating DataTokens and RouteValues unless we need to make a copy.
            if (other._dataTokens != null)
            {
                _dataTokens = new RouteValueDictionary(other._dataTokens);
            }

            if (other._values != null)
            {
                _values = new RouteValueDictionary(other._values);
            }
        }

        /// <summary>
        /// Gets the data tokens produced by routes on the current routing path.
        /// </summary>
        public RouteValueDictionary DataTokens
        {
            get
            {
                if (_dataTokens == null)
                {
                    _dataTokens = new RouteValueDictionary();
                }

                return _dataTokens;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IRouter"/> instances on the current routing path.
        /// </summary>
        public List<IRouter> Routers { get; }

        /// <summary>
        /// Gets the set of values produced by routes on the current routing path.
        /// </summary>
        public RouteValueDictionary Values
        {
            get
            {
                if (_values == null)
                {
                    _values = new RouteValueDictionary();
                }

                return _values;
            }
        }
    }
}