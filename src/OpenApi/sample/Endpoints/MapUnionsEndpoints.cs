// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

public static class UnionsEndpoints
{
    public static IEndpointRouteBuilder MapUnionsEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var unions = endpointRouteBuilder.MapGroup("/unions")
            .WithGroupName("unions");

        // Direct return: object-cased union.
        unions.MapGet("/pet", () => new UnionPet(new Kitten("Whiskers", 9)));

        // Direct return: primitive-cased union.
        unions.MapGet("/value", () => new UnionIntString(42));

        // Container record whose property is a union — exercises nested schema reuse.
        unions.MapGet("/clinic", () => new Clinic("123 Vet Ave", new UnionPet(new Puppy("Rex", "Beagle"))));

        // TypedResults wrapping a union response.
        unions.MapGet("/typed-result", () => TypedResults.Ok(new UnionPet(new Kitten("Mittens", 7))));

        // Request body: union as POST payload.
        unions.MapPost("/pet", ([FromBody] UnionPet pet) => TypedResults.Ok(pet));

        // Multi-Produces, same status + same content-type: union and non-union side-by-side.
        unions.MapGet("/any-of", () => Results.Ok())
            .Produces<UnionPet>(StatusCodes.Status200OK, "application/json")
            .Produces<Clinic>(StatusCodes.Status200OK, "application/json");

        // Multi-Produces, same status + same content-type: two distinct unions.
        unions.MapGet("/any-of-unions", () => Results.Ok())
            .Produces<UnionPet>(StatusCodes.Status200OK, "application/json")
            .Produces<UnionIntString>(StatusCodes.Status200OK, "application/json");

        // Multi-Produces, same status + different content-types: union JSON vs plain text.
        unions.MapGet("/multi-content-type", () => Results.Ok())
            .Produces<UnionPet>(StatusCodes.Status200OK, "application/json")
            .Produces<string>(StatusCodes.Status200OK, "text/plain");

        return endpointRouteBuilder;
    }
}
