// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// The result of matching a route. Includes an <see cref="IRouteEndpoint"/> to invoke and an optional collection of
    /// captured values.
    /// </summary>
    public sealed class RouteMatch
    {
        public RouteMatch(IRouteEndpoint endpoint)
            : this(endpoint, null)
        {
        }

        public RouteMatch(IRouteEndpoint endpoint, IDictionary<string, object> values)
        {
            Endpoint = endpoint;
            Values = values;
        }

        public IRouteEndpoint Endpoint
        {
            get;
            private set;
        }

        public IDictionary<string, object> Values
        {
            get;
            private set;
        }
    }
}
