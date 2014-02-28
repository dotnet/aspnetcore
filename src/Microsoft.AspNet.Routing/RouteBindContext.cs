// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public class RouteBindContext
    {
        public RouteBindContext(HttpContext context, IDictionary<string, object> values)
        {
            Context = context;
            Values = values;

            if (Context != null)
            {
                var ambientValues = context.GetFeature<IRouteValues>();
                AmbientValues = ambientValues == null ? null : ambientValues.Values;
            }
        }

        public IDictionary<string, object> AmbientValues { get; private set; } 

        public HttpContext Context { get; private set; }

        public IDictionary<string, object> Values { get; private set; } 
    }
}
