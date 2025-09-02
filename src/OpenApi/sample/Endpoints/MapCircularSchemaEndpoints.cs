// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Sample.Endpoints;

public static class CircularEndpointsExtensions
{
    public static IEndpointRouteBuilder MapCircularEndpoints1(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var circularSchemaRoutes = endpointRouteBuilder.MapGroup("circular1")
            .WithGroupName("circular1");

        circularSchemaRoutes.MapGet("/model", () => TypedResults.Ok(new CircularModel1()));

        return circularSchemaRoutes;
    }
    public static IEndpointRouteBuilder MapCircularEndpoints2(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var circularSchemaRoutes = endpointRouteBuilder.MapGroup("circular2")
            .WithGroupName("circular2");

        circularSchemaRoutes.MapGet("/model", () => TypedResults.Ok(new CircularModel2()));

        return circularSchemaRoutes;
    }

    public class CircularModel1
    {
        public ReferencedModel Referenced { get; set; } = null!;
        public CircularModel1 Self { get; set; } = null!;
    }

    public class CircularModel2
    {
        public CircularModel2 Self { get; set; } = null!;
        public ReferencedModel Referenced { get; set; } = null!;
    }

    public class ReferencedModel
    {
        public int Id { get; set; }
    }
}
