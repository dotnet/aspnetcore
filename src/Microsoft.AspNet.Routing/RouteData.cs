// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// Summary description for RouteData
    /// </summary>
    public class RouteData
    {
        public RouteData()
        {
            Routers = new List<IRouter>();
        }

        public List<IRouter> Routers { get; private set; }

        public IDictionary<string, object> Values { get; set; }

        public IDictionary<string, object> DataTokens { get; set; }
    }
}