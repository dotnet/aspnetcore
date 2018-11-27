// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class HealthChecksEndpointRouteBuilderExtensions
    {
        private static readonly Random _random = new Random();

        public static IEndpointConventionBuilder MapHealthChecks(this IEndpointRouteBuilder builder, string pattern)
        {
            return builder.MapGet(
                pattern,
                async httpContext =>
                {
                    await httpContext.Response.WriteAsync(_random.Next() % 2 == 0 ? "Up!" : "Down!");
                });
        }
    }
}
