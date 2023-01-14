// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapHello(this IEndpointRouteBuilder endpoints, string template, string greeter)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var pipeline = endpoints.CreateApplicationBuilder()
           .UseHello(greeter)
           .Build();

        return endpoints.Map(template, pipeline).WithDisplayName("Hello " + greeter);
    }
}
