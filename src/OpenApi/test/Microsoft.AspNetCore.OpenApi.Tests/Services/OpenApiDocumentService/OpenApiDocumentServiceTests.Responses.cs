// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiResponse_SupportsMultipleResponseViaAttributes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos",
            [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        () =>
            { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Collection(operation.Responses.OrderBy(r => r.Key),
                response =>
                {
                    Assert.Equal("201", response.Key);
                    Assert.Equal("Created", response.Value.Description);
                },
                response =>
                {
                    Assert.Equal("400", response.Key);
                    Assert.Equal("Bad Request", response.Value.Description);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsProblemDetailsResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(ProblemDetails), ["application/json+problem"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("400", response.Key);
            Assert.Equal("Bad Request", response.Value.Description);
            Assert.Equal("application/json+problem", response.Value.Content.Keys.Single());
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsMultipleResponsesForStatusCode()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            // Simulates metadata provided by IEndpointMetadataProvider
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK))
            // Simulates metadata added via `Produces` call
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(string), ["text/plain"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
            var content = Assert.Single(response.Value.Content);
            Assert.Equal("text/plain", content.Key);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsMultipleResponseTypesWithTypeForStatusCode()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            // Simulates metadata provided by IEndpointMetadataProvider
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]))
            // Simulates metadata added via `Produces` call
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(TodoWithDueDate), ["application/json"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);
            // Todo: Check that this generates a schema using `oneOf`.
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsMultipleResponseTypesWitDifferentContentTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json", "application/xml"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
            Assert.Collection(response.Value.Content.OrderBy(c => c.Key),
                content =>
                {
                    Assert.Equal("application/json", content.Key);
                },
                content =>
                {
                    Assert.Equal("application/xml", content.Key);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsDifferentResponseTypesWitDifferentContentTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(TodoWithDueDate), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/xml"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
            Assert.Collection(response.Value.Content.OrderBy(c => c.Key),
                content =>
                {
                    Assert.Equal("application/xml", content.Key);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_ProducesDefaultResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsMvcProducesAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", [Produces("application/json", "application/xml")] () => new Todo(1, "Test todo", false, DateTime.Now));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal("OK", response.Value.Description);
            Assert.Collection(response.Value.Content.OrderBy(c => c.Key),
                content =>
                {
                    Assert.Equal("application/json", content.Key);
                },
                content =>
                {
                    Assert.Equal("application/xml", content.Key);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsGeneratingDefaultResponseField()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", [ProducesDefaultResponseType(typeof(Error))] () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal(Microsoft.AspNetCore.OpenApi.OpenApiConstants.DefaultOpenApiResponseKey, response.Key);
            Assert.Empty(response.Value.Description);
            var mediaTypeEntry = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", mediaTypeEntry.Key);
            var schema = mediaTypeEntry.Value.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties, property =>
            {
                Assert.Equal("code", property.Key);
                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
            }, property =>
            {
                Assert.Equal("message", property.Key);
                Assert.Equal( JsonSchemaType.String, property.Value.Type);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsGeneratingDefaultResponseWithSuccessResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", [ProducesDefaultResponseType(typeof(Error))] () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var defaultResponse = operation.Responses[Microsoft.AspNetCore.OpenApi.OpenApiConstants.DefaultOpenApiResponseKey];
            // Generates a default response with the `Error` type.
            Assert.NotNull(defaultResponse);
            Assert.Empty(defaultResponse.Description);
            var defaultContent = Assert.Single(defaultResponse.Content.Values);
            var defaultSchema = defaultContent.Schema;
            Assert.Collection(defaultSchema.Properties,
            property =>
            {
                Assert.Equal("code", property.Key);
                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
            },
            property =>
            {
                Assert.Equal("message", property.Key);
                Assert.Equal( JsonSchemaType.String, property.Value.Type);
            });
            // Generates the 200 status code response with the `Todo` response type.
            var okResponse = operation.Responses["200"];
            Assert.NotNull(okResponse);
            Assert.Equal("OK", okResponse.Description);
            var okContent = Assert.Single(okResponse.Content);
            Assert.Equal("application/json", okContent.Key);
            var schema = okContent.Value.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties, property =>
            {
                Assert.Equal("id", property.Key);
                Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
            }, property =>
            {
                Assert.Equal("title", property.Key);
                Assert.Equal(JsonSchemaType.String, property.Value.Type);
            }, property =>
            {
                Assert.Equal("completed", property.Key);
                Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
            }, property =>
            {
                Assert.Equal("createdAt", property.Key);
                Assert.Equal(JsonSchemaType.String, property.Value.Type);
                Assert.Equal("date-time", property.Value.Format);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesDescriptionSetByUser()
    {
        // Arrange
        var builder = CreateBuilder();

        const string expectedCreatedDescription = "A new todo item was created";
        const string expectedBadRequestDescription = "Validation failed for the request";

        // Act
        builder.MapGet("/api/todos",
            [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created, Description = expectedCreatedDescription)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = expectedBadRequestDescription)]
        () =>
            { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Collection(operation.Responses.OrderBy(r => r.Key),
                response =>
                {
                    Assert.Equal("201", response.Key);
                    Assert.Equal(expectedCreatedDescription, response.Value.Description);
                },
                response =>
                {
                    Assert.Equal("400", response.Key);
                    Assert.Equal(expectedBadRequestDescription, response.Value.Description);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesStatusCodeReasonPhraseWhenExplicitDescriptionIsMissing()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos",
            [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created, Description = null)] // Explicitly set to NULL
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Omitted, meaning it should be NULL
        () =>
            { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Collection(operation.Responses.OrderBy(r => r.Key),
                response =>
                {
                    Assert.Equal("201", response.Key);
                    Assert.Equal("Created", response.Value.Description);
                },
                response =>
                {
                    Assert.Equal("400", response.Key);
                    Assert.Equal("Bad Request", response.Value.Description);
                });
        });
    }
}
