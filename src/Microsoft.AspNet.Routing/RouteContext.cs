// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public class RouteContext
    {
        public RouteContext(HttpContext context)
        {
            Context = context;

            RequestPath = context.Request.Path.Value;
        }

        public HttpContext Context
        {
            get;
            private set;
        }

        public string RequestPath
        {
            get;
            private set;
        }
    }
}
