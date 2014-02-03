// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class RouteValues : IRouteValues
    {
        public RouteValues(IDictionary<string, object> values)
        {
            Values = values;
        }

        public IDictionary<string, object> Values
        {
            get;
            private set;
        }
    }
}
