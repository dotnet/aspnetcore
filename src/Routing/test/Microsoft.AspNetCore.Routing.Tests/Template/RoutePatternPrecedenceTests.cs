// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    public class RoutePatternPrecedenceTests : RoutePrecedenceTestsBase
    {
        protected override decimal ComputeMatched(string template)
        {
            return ComputeRoutePattern(template, RoutePrecedence.ComputeInbound);
        }

        protected override decimal ComputeGenerated(string template)
        {
            return ComputeRoutePattern(template, RoutePrecedence.ComputeOutbound);
        }

        private static decimal ComputeRoutePattern(string template, Func<RoutePattern, decimal> func)
        {
            var parsed = RoutePatternFactory.Parse(template);
            return func(parsed);
        }
    }
}
