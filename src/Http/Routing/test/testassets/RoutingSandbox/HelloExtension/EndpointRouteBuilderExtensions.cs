// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapHello(this IEndpointRouteBuilder endpoints, string pattern, string greeter)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var pipeline = endpoints.CreateApplicationBuilder()
           .UseHello(greeter)
           .Build();

        return endpoints.Map(pattern, pipeline);
    }
}
