// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class RouteData
    {
        public RouteData()
        {
            DataTokens = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Routers = new List<IRouter>();
            Values = new RouteValueDictionary();
        }

        public RouteData([NotNull] RouteData other)
        {
            DataTokens = new Dictionary<string, object>(other.DataTokens, StringComparer.OrdinalIgnoreCase);
            Routers = new List<IRouter>(other.Routers);
            Values = new RouteValueDictionary(other.Values);
        }

        public List<IRouter> Routers { get; private set; }

        public IDictionary<string, object> Values { get; private set; }

        public IDictionary<string, object> DataTokens { get; private set; }
    }
}