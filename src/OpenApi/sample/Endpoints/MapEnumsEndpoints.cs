// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

public static class EnumsEndpointsExtensions
{
    public static IEndpointRouteBuilder MapEnumsEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        // Each scenario in the enum matrix (naming policy x nullability x parameter source)
        // is exposed as its own OpenAPI document via a dedicated group name.

        // Non-body (query) parameter scenarios.
        endpointRouteBuilder.MapGet("/enums/pascalcase/nonnullable-param", (PascalCaseStatus status) => TypedResults.Ok(status))
            .WithGroupName("enum-pascalcase-nonnullable-param");
        endpointRouteBuilder.MapGet("/enums/pascalcase/nullable-param", (PascalCaseStatus? status) => TypedResults.Ok(status))
            .WithGroupName("enum-pascalcase-nullable-param");
        endpointRouteBuilder.MapGet("/enums/camelcase/nonnullable-param", (CamelCaseStatus status) => TypedResults.Ok(status))
            .WithGroupName("enum-camelcase-nonnullable-param");
        endpointRouteBuilder.MapGet("/enums/camelcase/nullable-param", (CamelCaseStatus? status) => TypedResults.Ok(status))
            .WithGroupName("enum-camelcase-nullable-param");

        // Body parameter scenarios where the enum is a property of a model.
        endpointRouteBuilder.MapPost("/enums/pascalcase/nonnullable-body-model", (PascalCaseNonNullableModel model) => TypedResults.Ok(model))
            .WithGroupName("enum-pascalcase-nonnullable-body-model");
        endpointRouteBuilder.MapPost("/enums/pascalcase/nullable-body-model", (PascalCaseNullableModel model) => TypedResults.Ok(model))
            .WithGroupName("enum-pascalcase-nullable-body-model");
        endpointRouteBuilder.MapPost("/enums/camelcase/nonnullable-body-model", (CamelCaseNonNullableModel model) => TypedResults.Ok(model))
            .WithGroupName("enum-camelcase-nonnullable-body-model");
        endpointRouteBuilder.MapPost("/enums/camelcase/nullable-body-model", (CamelCaseNullableModel model) => TypedResults.Ok(model))
            .WithGroupName("enum-camelcase-nullable-body-model");

        // Body parameter scenarios where the enum is the request body directly.
        endpointRouteBuilder.MapPost("/enums/pascalcase/nonnullable-body-direct", ([FromBody] PascalCaseStatus status) => TypedResults.Ok(status))
            .WithGroupName("enum-pascalcase-nonnullable-body-direct");
        endpointRouteBuilder.MapPost("/enums/pascalcase/nullable-body-direct", ([FromBody] PascalCaseStatus? status) => TypedResults.Ok(status))
            .WithGroupName("enum-pascalcase-nullable-body-direct");
        endpointRouteBuilder.MapPost("/enums/camelcase/nonnullable-body-direct", ([FromBody] CamelCaseStatus status) => TypedResults.Ok(status))
            .WithGroupName("enum-camelcase-nonnullable-body-direct");
        endpointRouteBuilder.MapPost("/enums/camelcase/nullable-body-direct", ([FromBody] CamelCaseStatus? status) => TypedResults.Ok(status))
            .WithGroupName("enum-camelcase-nullable-body-direct");

        return endpointRouteBuilder;
    }

    // Serialized with the default (no) naming policy, preserving the PascalCase member names.
    [JsonConverter(typeof(JsonStringEnumConverter<PascalCaseStatus>))]
    public enum PascalCaseStatus
    {
        Active,
        Inactive,
        Pending
    }

    // Serialized with the camelCase naming policy applied to the member names.
    [JsonConverter(typeof(CamelCaseStatusConverter))]
    public enum CamelCaseStatus
    {
        Active,
        Inactive,
        Pending
    }

    private sealed class CamelCaseStatusConverter() : JsonStringEnumConverter<CamelCaseStatus>(JsonNamingPolicy.CamelCase);

    public sealed class PascalCaseNonNullableModel
    {
        public required PascalCaseStatus Status { get; set; }
    }

    public sealed class PascalCaseNullableModel
    {
        public PascalCaseStatus? Status { get; set; }
    }

    public sealed class CamelCaseNonNullableModel
    {
        public required CamelCaseStatus Status { get; set; }
    }

    public sealed class CamelCaseNullableModel
    {
        public CamelCaseStatus? Status { get; set; }
    }
}
