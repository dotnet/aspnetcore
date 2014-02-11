// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing
{
    public class RouteBuilder : IRouteBuilder
    {
        public RouteBuilder(IRouteEndpoint endpoint, IRouteCollection routes)
        {
            Endpoint = endpoint;
            Routes = routes;
        }
        public IRouteEndpoint Endpoint
        {
            get;
            private set;
        }

        public IRouteCollection Routes
        {
            get;
            private set;
        }
    }
}
