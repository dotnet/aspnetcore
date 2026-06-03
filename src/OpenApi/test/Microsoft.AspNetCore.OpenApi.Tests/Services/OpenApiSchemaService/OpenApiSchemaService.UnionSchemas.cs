// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiResponse_SingleUnionResponse_EmitsRefToComponentWithAnyOf()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pet", () => new UnionPet(new Kitten("Whiskers", 9)));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/pet"].Operations[HttpMethod.Get];
            var response = Assert.Single(operation.Responses).Value;
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);

            var schema = content.Value.Schema;
            var schemaRef = Assert.IsType<OpenApiSchemaReference>(schema);
            Assert.Equal(nameof(UnionPet), schemaRef.Reference.Id);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_UnionWithPrimitives_EmitsAnyOfOverPrimitiveSchemas()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/value", () => new UnionIntString(42));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/value"].Operations[HttpMethod.Get];
            var response = Assert.Single(operation.Responses).Value;
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);

            var schema = content.Value.Schema;
            var schemaRef = Assert.IsType<OpenApiSchemaReference>(schema);
            Assert.Equal(nameof(UnionIntString), schemaRef.Reference.Id);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionIntString), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);

            Assert.Contains(unionComponent.AnyOf, branch => branch.Type == JsonSchemaType.Integer);
            Assert.Contains(unionComponent.AnyOf, branch => branch.Type == JsonSchemaType.String);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_ContainerWithUnionProperty_PropertyExposesUnionAnyOf()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/clinic", () => new Clinic("123 Vet Ave", new UnionPet(new Puppy("Rex", "Beagle"))));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/clinic"].Operations[HttpMethod.Get];
            var response = Assert.Single(operation.Responses).Value;
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);

            var responseSchema = content.Value.Schema;
            var clinicRef = Assert.IsType<OpenApiSchemaReference>(responseSchema);
            Assert.Equal(nameof(Clinic), clinicRef.Reference.Id);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(Clinic), out var clinicComponent));
            Assert.Equal(JsonSchemaType.Object, clinicComponent.Type);

            Assert.True(clinicComponent.Properties.TryGetValue("patient", out var patientProperty));
            // The Patient property should resolve to the UnionPet schema, either as a
            // direct $ref to the registered component or by exposing the union's anyOf.
            if (patientProperty is OpenApiSchemaReference patientRef)
            {
                Assert.Equal(nameof(UnionPet), patientRef.Reference.Id);
                Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
                Assert.NotNull(unionComponent.AnyOf);
                Assert.Equal(2, unionComponent.AnyOf.Count);
            }
            else
            {
                Assert.NotNull(patientProperty.AnyOf);
                Assert.Equal(2, patientProperty.AnyOf.Count);
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequest_RequestBodyUnion_EmitsRefToComponentWithAnyOf()
    {
        var builder = CreateBuilder();

        builder.MapPost("/api/pet", ([FromBody] UnionPet pet) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/pet"].Operations[HttpMethod.Post];
            Assert.NotNull(operation.RequestBody);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json", content.Key);

            var schema = content.Value.Schema;
            var schemaRef = Assert.IsType<OpenApiSchemaReference>(schema);
            Assert.Equal(nameof(UnionPet), schemaRef.Reference.Id);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_TypedResultsReturningUnion_EmitsRefToUnionComponent()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pet", () => TypedResults.Ok(new UnionPet(new Kitten("Whiskers", 9))));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/pet"].Operations[HttpMethod.Get];
            var response = Assert.Single(operation.Responses).Value;
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);

            var schema = content.Value.Schema;
            var schemaRef = Assert.IsType<OpenApiSchemaReference>(schema);
            Assert.Equal(nameof(UnionPet), schemaRef.Reference.Id);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);
        });
    }
}
