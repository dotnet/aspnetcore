// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    internal static class CircuitsEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapStartCircuitEndpoint(this IEndpointRouteBuilder builder, string pattern)
        {
            var circuitIdFactory = builder.ServiceProvider.GetRequiredService<CircuitIdFactory>();
            var actions = new CircuitEndpoints(circuitIdFactory);
            IApplicationBuilder start = builder.CreateApplicationBuilder();
            start.Run(actions.StartCircuitAsync);
            IEndpointConventionBuilder startBuilder = builder.Map(pattern, start.Build());
            return startBuilder;
        }
    }
}
