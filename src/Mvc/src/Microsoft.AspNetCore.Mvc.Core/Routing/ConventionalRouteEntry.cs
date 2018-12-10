// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ConventionalRouteEntry
    {
        public ConventionalRouteEntry(
            string name, 
            string template, 
            RouteValueDictionary defaults, 
            RouteValueDictionary constraints, 
            RouteValueDictionary dataTokens,
            int order)
        {
            Name = name;
            Template = template;
            Defaults = defaults;
            Constraints = constraints;
            DataTokens = dataTokens;
            Order = order;

            Pattern = RoutePatternFactory.Parse(template, defaults, constraints);
        }

        public string Name { get; }

        public string Template { get; }

        public RoutePattern Pattern { get; }

        public RouteValueDictionary Defaults { get; }

        public RouteValueDictionary Constraints { get; }

        public RouteValueDictionary DataTokens { get; }

        public int Order { get; }
    }
}
