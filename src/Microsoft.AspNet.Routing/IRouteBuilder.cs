// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing
{
    public interface IRouteBuilder
    {
        IRouteEndpoint Endpoint
        {
            get;
        }

        IRouteCollection Routes
        {
            get;
        }
    }
}
