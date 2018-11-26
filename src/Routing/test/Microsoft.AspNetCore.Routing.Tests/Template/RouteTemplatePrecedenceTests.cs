// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Template
{
    public class RouteTemplatePrecedenceTests : RoutePrecedenceTestsBase
    {
        protected override decimal ComputeMatched(string template)
        {
            return ComputeRouteTemplate(template, RoutePrecedence.ComputeInbound);
        }

        protected override decimal ComputeGenerated(string template)
        {
            return ComputeRouteTemplate(template, RoutePrecedence.ComputeOutbound);
        }

        private static decimal ComputeRouteTemplate(string template, Func<RouteTemplate, decimal> func)
        {
            var parsed = TemplateParser.Parse(template);
            return func(parsed);
        }
    }
}
