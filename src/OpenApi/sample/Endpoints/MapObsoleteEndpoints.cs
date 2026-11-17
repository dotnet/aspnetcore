// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class ObsoleteEndpointsExtensions
{
    public static IEndpointRouteBuilder MapObsoleteEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var obsolete = endpointRouteBuilder.MapGroup("obsolete")
            .WithGroupName("obsolete");

        obsolete.MapPost("/deprecated", (RequestWithObsoleteMembers request) => TypedResults.Ok(request))
            .WithMetadata(new ObsoleteAttribute());
        obsolete.MapGet("/current", () => TypedResults.Ok(new CurrentResponse { Value = "current" }));

        return endpointRouteBuilder;
    }

    public sealed class RequestWithObsoleteMembers
    {
        public required string Current { get; set; }

        [Obsolete]
        public string Legacy { get; set; } = string.Empty;

        public required ReusableType CurrentReference { get; set; }

        [Obsolete]
        public ReusableType LegacyReference { get; set; } = new() { Value = string.Empty };

#pragma warning disable CS0612 // Type or member is obsolete
        public required ObsoleteType ObsoleteType { get; set; }
#pragma warning restore CS0612
    }

    public sealed class ReusableType
    {
        public required string Value { get; set; }
    }

    public sealed class CurrentResponse
    {
        public required string Value { get; set; }
    }

    [Obsolete]
    public sealed class ObsoleteType
    {
        public required string Value { get; set; }
    }
}
