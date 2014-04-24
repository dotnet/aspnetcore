﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public class VirtualPathContext
    {
        public VirtualPathContext(HttpContext httpContext,
                                  IDictionary<string, object> ambientValues, 
                                  IDictionary<string, object> values)
            : this(httpContext, ambientValues, values, null)
        {
        }

        public VirtualPathContext(HttpContext context,
                                  IDictionary<string, object> ambientValues,
                                  IDictionary<string, object> values,
                                  string routeName)
        {
            Context = context;
            AmbientValues = ambientValues;
            Values = values;
            RouteName = routeName;
        }

        public string RouteName { get; private set; }

        public IDictionary<string, object> ProvidedValues { get; set; } 

        public IDictionary<string, object> AmbientValues { get; private set; } 

        public HttpContext Context { get; private set; }

        public bool IsBound { get; set; }

        public IDictionary<string, object> Values { get; private set; } 
    }
}
