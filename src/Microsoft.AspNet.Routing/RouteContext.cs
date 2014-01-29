// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public sealed class RouteContext
    {
        public RouteContext(IDictionary<string, object> context)
        {
            Context = context;

            RequestPath = (string)context["owin.RequestPath"];
        }

        public IDictionary<string, object> Context
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
