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
        public static IEndpointConventionBuilder MapHello(this IEndpointRouteBuilder builder, string template, string greeter)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var pipeline = builder.CreateApplicationBuilder()
               .UseHello(greeter)
               .Build();

            return builder.Map(
                template,
                "Hello " + greeter,
                pipeline);
        }
    }
}
