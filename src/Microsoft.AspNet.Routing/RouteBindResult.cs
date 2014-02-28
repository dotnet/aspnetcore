// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing
{
    public class RouteBindResult
    {
        public RouteBindResult(string url)
        {
            Url = url;
        }

        public string Url { get; private set; }
    }
}
