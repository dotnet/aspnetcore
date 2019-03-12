// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHello(this IEndpointRouteBuilder routes, string template, string greeter)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            var pipeline = routes.CreateApplicationBuilder()
               .UseHello(greeter)
               .Build();

            return routes.Map(
                template,
                "Hello " + greeter,
                pipeline);
        }
    }
}
