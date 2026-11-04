// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiResponse_UnionAndNonUnion_SameStatus_SameContentType_MergesIntoAnyOfWithoutFlattening()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pet-or-clinic", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(UnionPet), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Clinic), ["application/json"]));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/pet-or-clinic"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);

            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);

            var outerSchema = content.Value.Schema;
            Assert.NotNull(outerSchema.AnyOf);
            Assert.Equal(2, outerSchema.AnyOf.Count);

            var referencedIds = outerSchema.AnyOf
                .OfType<OpenApiSchemaReference>()
                .Select(s => s.Reference.Id)
                .ToArray();
            Assert.Contains(nameof(UnionPet), referencedIds);
            Assert.Contains(nameof(Clinic), referencedIds);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_TwoUnions_SameStatus_SameContentType_MergesIntoAnyOfWithoutFlattening()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/either-union", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(UnionPet), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(UnionIntString), ["application/json"]));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/either-union"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);

            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);

            var outerSchema = content.Value.Schema;
            Assert.NotNull(outerSchema.AnyOf);
            Assert.Equal(2, outerSchema.AnyOf.Count);

            var referencedIds = outerSchema.AnyOf
                .OfType<OpenApiSchemaReference>()
                .Select(s => s.Reference.Id)
                .ToArray();
            Assert.Contains(nameof(UnionPet), referencedIds);
            Assert.Contains(nameof(UnionIntString), referencedIds);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionPetComponent));
            Assert.NotNull(unionPetComponent.AnyOf);
            Assert.Equal(2, unionPetComponent.AnyOf.Count);

            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionIntString), out var unionIntStringComponent));
            Assert.NotNull(unionIntStringComponent.AnyOf);
            Assert.Equal(2, unionIntStringComponent.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_UnionAndNonUnion_SameStatus_DifferentContentTypes_KeepsThemSeparate()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pet-or-error", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(UnionPet), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Error), ["text/plain"]));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/pet-or-error"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal(2, response.Value.Content.Count);

            // application/json must be a direct $ref to UnionPet — not wrapped in an outer
            // anyOf with the text/plain branch (the two content types must stay separate).
            Assert.True(response.Value.Content.TryGetValue("application/json", out var jsonContent));
            var jsonRef = Assert.IsType<OpenApiSchemaReference>(jsonContent.Schema);
            Assert.Equal(nameof(UnionPet), jsonRef.Reference.Id);

            // Peek through the ref into the actual UnionPet component to verify the union
            // shape (anyOf over Kitten + Puppy) is preserved as-is. Union case branches
            // are promoted to top-level components using the case type's own name (e.g. "Kitten"),
            // not a parent-prefixed name, so a standalone Kitten endpoint can share the same component.
            Assert.True(document.Components.Schemas.TryGetValue(nameof(UnionPet), out var unionComponent));
            Assert.NotNull(unionComponent.AnyOf);
            Assert.Equal(2, unionComponent.AnyOf.Count);
            var unionBranchIds = unionComponent.AnyOf
                .OfType<OpenApiSchemaReference>()
                .Select(s => s.Reference.Id)
                .ToArray();
            Assert.Contains(nameof(Kitten), unionBranchIds);
            Assert.Contains(nameof(Puppy), unionBranchIds);

            // text/plain must be a direct $ref to Error — independent of the JSON branch.
            Assert.True(response.Value.Content.TryGetValue("text/plain", out var textContent));
            var textRef = Assert.IsType<OpenApiSchemaReference>(textContent.Schema);
            Assert.Equal(nameof(Error), textRef.Reference.Id);

            // Peek through the ref into the actual Error component to verify it's an object,
            // not accidentally wrapped in an anyOf alongside UnionPet.
            Assert.True(document.Components.Schemas.TryGetValue(nameof(Error), out var errorComponent));
            Assert.Null(errorComponent.AnyOf);
            Assert.Equal(JsonSchemaType.Object, errorComponent.Type);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_ProducesBuilder_UnionAndNonUnion_SameStatus_SameContentType_MergesIntoAnyOf()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pet-or-clinic", () => Results.Ok())
            .Produces<UnionPet>(StatusCodes.Status200OK, "application/json")
            .Produces<Clinic>(StatusCodes.Status200OK, "application/json");

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/pet-or-clinic"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);

            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);

            var outerSchema = content.Value.Schema;
            Assert.NotNull(outerSchema.AnyOf);
            Assert.Equal(2, outerSchema.AnyOf.Count);

            var referencedIds = outerSchema.AnyOf
                .OfType<OpenApiSchemaReference>()
                .Select(s => s.Reference.Id)
                .ToArray();
            Assert.Contains(nameof(UnionPet), referencedIds);
            Assert.Contains(nameof(Clinic), referencedIds);
        });
    }
}
