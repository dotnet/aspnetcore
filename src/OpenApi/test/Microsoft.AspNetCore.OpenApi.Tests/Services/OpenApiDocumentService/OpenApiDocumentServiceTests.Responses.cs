// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.ServerSentEvents;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    public async Task GetOpenApiResponse_UsesItemSchemaForServerSentEvents()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/todos/events", () => TypedResults.ServerSentEvents(GetEvents()));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos/events"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            var content = Assert.Single(response.Value.Content);
            Assert.Equal("text/event-stream", content.Key);
            Assert.Null(content.Value.Schema);
            var itemSchema = Assert.IsType<OpenApiSchema>(content.Value.ItemSchema);
            Assert.Equal(JsonSchemaType.Object, itemSchema.Type);
            Assert.Equal(["data"], itemSchema.Required);
            Assert.Collection(itemSchema.Properties.OrderBy(property => property.Key),
                property =>
                {
                    Assert.Equal("data", property.Key);
                    var dataSchema = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal(nameof(Todo), dataSchema.Reference.Id);
                },
                property =>
                {
                    Assert.Equal("event", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Null(property.Value.Enum);
                },
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                });
        });

        static async IAsyncEnumerable<SseItem<Todo>> GetEvents()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesItemSchemaForServerSentEventsOfDataItems()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/todos/data-events", () => TypedResults.ServerSentEvents(GetEvents()));

        await VerifyOpenApiDocument(builder, document =>
        {
            var itemSchema = GetServerSentEventsItemSchema(document, "/api/todos/data-events");
            var dataSchema = Assert.IsType<OpenApiSchemaReference>(itemSchema.Properties["data"]);
            Assert.Equal(nameof(Todo), dataSchema.Reference.Id);
        });

        static async IAsyncEnumerable<Todo> GetEvents()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesItemSchemaForServerSentEventsOfStringItems()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/messages/events", () => TypedResults.ServerSentEvents(GetEvents()));

        await VerifyOpenApiDocument(builder, document =>
        {
            var itemSchema = GetServerSentEventsItemSchema(document, "/api/messages/events");
            var dataSchema = Assert.IsType<OpenApiSchema>(itemSchema.Properties["data"]);
            Assert.Equal(JsonSchemaType.String, dataSchema.Type);
        });

        static async IAsyncEnumerable<string> GetEvents()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesSchemaAndItemSchemaForSameDataSchemaInDifferentMediaTypes()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/todos/responses", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(SseItem<Todo>), ["text/event-stream"]));

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos/responses"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal(2, response.Value.Content.Count);

            var jsonContent = response.Value.Content["application/json"];
            var jsonSchema = Assert.IsType<OpenApiSchemaReference>(jsonContent.Schema);
            Assert.Equal(nameof(Todo), jsonSchema.Reference.Id);
            Assert.Null(jsonContent.ItemSchema);

            var eventStreamContent = response.Value.Content["text/event-stream"];
            Assert.Null(eventStreamContent.Schema);
            var itemSchema = Assert.IsType<OpenApiSchema>(eventStreamContent.ItemSchema);
            var dataSchema = Assert.IsType<OpenApiSchemaReference>(itemSchema.Properties["data"]);
            Assert.Equal(nameof(Todo), dataSchema.Reference.Id);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesDiscriminatedUnionCasesForServerSentEventsEventSchema()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/pets/events", () => TypedResults.ServerSentEvents(GetEvents()));

        await VerifyOpenApiDocument(builder, document =>
        {
            var itemSchema = GetServerSentEventsItemSchema(document, "/api/pets/events");
            var dataSchema = Assert.IsType<OpenApiSchemaReference>(itemSchema.Properties["data"]);
            Assert.Equal(nameof(UnionPet), dataSchema.Reference.Id);
            AssertEventSchema(itemSchema, [nameof(Kitten), nameof(Puppy)]);
        });

        static async IAsyncEnumerable<SseItem<UnionPet>> GetEvents()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task GetOpenApiResponse_UsesAbstractBaseDiscriminatorValuesForServerSentEventsEventSchema()
    {
        var builder = CreateBuilder();

        builder.MapGet("/api/vehicles/events", () => TypedResults.ServerSentEvents(GetEvents()));

        await VerifyOpenApiDocument(builder, document =>
        {
            var itemSchema = GetServerSentEventsItemSchema(document, "/api/vehicles/events");
            var dataSchema = Assert.IsType<OpenApiSchemaReference>(itemSchema.Properties["data"]);
            Assert.Equal(nameof(Vehicle), dataSchema.Reference.Id);
            AssertEventSchema(itemSchema, ["car", "truck", "plane"]);
        });

        static async IAsyncEnumerable<SseItem<Vehicle>> GetEvents()
        {
            await Task.CompletedTask;
            yield break;
        }
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
            var schema = content.Value.Schema;
            Assert.NotNull(schema.AnyOf);
            Assert.Equal(2, schema.AnyOf.Count);
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
                    Assert.Equal("application/json", content.Key);
                },
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
                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
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
                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
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
                Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
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

    /// <remarks>
    /// Regression test for https://github.com/dotnet/aspnetcore/issues/60518
    /// </remarks>
    [Fact]
    public async Task GetOpenApiResponse_WithEmptyMethodBody_UsesDescriptionSetByUser()
    {
        // Arrange
        var builder = CreateBuilder();

        const string expectedCreatedDescription = "A new todo item was created";
        const string expectedBadRequestDescription = "Validation failed for the request";

        // Act
        builder.MapPost("/api/todos",
            [ProducesResponseType<Todo>(StatusCodes.Status200OK, Description = expectedCreatedDescription)]
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
                    Assert.Equal("200", response.Key);
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
    public async Task GetOpenApiResponse_UsesDescriptionSetByUser()
    {
        // Arrange
        var builder = CreateBuilder();

        const string expectedCreatedDescription = "A new todo item was created";
        const string expectedBadRequestDescription = "Validation failed for the request";

        // Act
        builder.MapPost("/api/todos",
            [ProducesResponseType<Todo>(StatusCodes.Status200OK, Description = expectedCreatedDescription)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = expectedBadRequestDescription)]
        () =>
            { return TypedResults.Ok(new Todo(1, "Lorem", true, DateTime.UtcNow)); }); // This code doesn't return Bad Request, but that doesn't matter for this test.

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Collection(operation.Responses.OrderBy(r => r.Key),
                response =>
                {
                    Assert.Equal("200", response.Key);
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
        builder.MapPost("/api/todos",
            [ProducesResponseType<Todo>(StatusCodes.Status200OK, Description = null)] // Explicitly set to NULL
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Omitted, meaning it should be NULL
        () =>
            { return TypedResults.Ok(new Todo(1, "Lorem", true, DateTime.UtcNow)); }); // This code doesn't return Bad Request, but that doesn't matter for this test.

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Collection(operation.Responses.OrderBy(r => r.Key),
                response =>
                {
                    Assert.Equal("200", response.Key);
                    Assert.Equal("OK", response.Value.Description);
                },
                response =>
                {
                    Assert.Equal("400", response.Key);
                    Assert.Equal("Bad Request", response.Value.Description);
                });
        });
    }

    /// <remarks>
    /// Regression test for https://github.com/dotnet/aspnetcore/issues/60518
    /// </remarks>
    [Fact]
    public async Task GetOpenApiResponse_WithEmptyMethodBody_UsesStatusCodeReasonPhraseWhenExplicitDescriptionIsMissing()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api/todos",
            [ProducesResponseType<Todo>(StatusCodes.Status200OK, Description = null)] // Explicitly set to NULL
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
                    Assert.Equal("200", response.Key);
                    Assert.Equal("OK", response.Value.Description);
                },
                response =>
                {
                    Assert.Equal("400", response.Key);
                    Assert.Equal("Bad Request", response.Value.Description);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_MergesMultipleTypesForSameContentTypeAndDifferentContentTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(TodoWithDueDate), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Error), ["text/plain"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal(2, response.Value.Content.Count);

            // application/json should have an anyOf schema since two types share the same content-type
            Assert.True(response.Value.Content.TryGetValue("application/json", out var jsonContent));
            Assert.NotNull(jsonContent.Schema.AnyOf);
            Assert.Equal(2, jsonContent.Schema.AnyOf.Count);

            // text/plain should have its own schema without anyOf
            Assert.True(response.Value.Content.TryGetValue("text/plain", out var textContent));
            Assert.Null(textContent.Schema.AnyOf);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsThreeTypesForSameContentTypeWithAnyOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(TodoWithDueDate), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Error), ["application/json"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema.AnyOf);
            Assert.Equal(3, content.Value.Schema.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_MultipleProducesWithDifferentStatusCodes_ProducesSeparateResponses()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { })
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Todo), ["application/json"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(Error), ["text/plain"]))
            .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(Error), ["application/json"]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            Assert.Equal(2, operation.Responses.Count);

            // 200 response should have both content types merged
            Assert.True(operation.Responses.TryGetValue("200", out var okResponse));
            Assert.Equal(2, okResponse.Content.Count);
            Assert.True(okResponse.Content.ContainsKey("application/json"));
            Assert.True(okResponse.Content.ContainsKey("text/plain"));

            // 404 response is separate
            Assert.True(operation.Responses.TryGetValue("404", out var notFoundResponse));
            var notFoundContent = Assert.Single(notFoundResponse.Content);
            Assert.Equal("application/json", notFoundContent.Key);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_ProducesExtensionMethod_SupportsDifferentTypesForSameStatusCode()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => Results.Ok())
            .Produces<Todo>(StatusCodes.Status200OK, "application/json")
            .Produces<Error>(StatusCodes.Status200OK, "text/plain");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Equal(2, response.Value.Content.Count);
            Assert.True(response.Value.Content.ContainsKey("application/json"));
            Assert.True(response.Value.Content.ContainsKey("text/plain"));
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_ProducesExtensionMethod_SupportsAnyOfForSameContentType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => Results.Ok())
            .Produces<Todo>(StatusCodes.Status200OK, "application/json")
            .Produces<Error>(StatusCodes.Status200OK, "application/json");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/api/todos"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            var content = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema.AnyOf);
            Assert.Equal(2, content.Value.Schema.AnyOf.Count);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_WithNumberAsString_ShouldSerializeWithoutErrors()
    {
        // Arrange
        var builder = CreateBuilder(serviceCollection: null, numberHandling: JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString);

        // Act
        builder.MapGet("/myapi", () => new ModelWithStringAndStringLength(string.Empty));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = Assert.Single(document.Paths["/myapi"].Operations.Values);
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            var kvp = Assert.Single(response.Value.Content);
            Assert.Equal("application/json", kvp.Key);
            var schema = Assert.Single(document.Components.Schemas);
            Assert.Equal(nameof(ModelWithStringAndStringLength), schema.Key);
            var property = Assert.Single(schema.Value.Properties);
            Assert.Equal("value", property.Key);
            Assert.Equal(100, property.Value.MaxLength);
        });
    }

    private static OpenApiSchema GetServerSentEventsItemSchema(OpenApiDocument document, string path)
    {
        var operation = Assert.Single(document.Paths[path].Operations.Values);
        var response = Assert.Single(operation.Responses);
        Assert.Equal("200", response.Key);
        var content = Assert.Single(response.Value.Content);
        Assert.Equal("text/event-stream", content.Key);
        Assert.Null(content.Value.Schema);
        return Assert.IsType<OpenApiSchema>(content.Value.ItemSchema);
    }

    private static void AssertEventSchema(OpenApiSchema itemSchema, string[] expectedEvents)
    {
        var eventSchema = Assert.IsType<OpenApiSchema>(itemSchema.Properties["event"]);
        Assert.Equal(JsonSchemaType.String, eventSchema.Type);
        Assert.NotNull(eventSchema.Enum);
        Assert.Equal(expectedEvents.OrderBy(value => value), eventSchema.Enum.Select(value => value.GetValue<string>()).OrderBy(value => value));
    }

    [JsonDerivedType(typeof(Car), typeDiscriminator: "car")]
    [JsonDerivedType(typeof(Truck), typeDiscriminator: "truck")]
    [JsonDerivedType(typeof(Plane), typeDiscriminator: "plane")]
    private abstract class Vehicle
    {
        public required string Make { get; set; }
    }

    private sealed class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    private sealed class Truck : Vehicle
    {
        public double PayloadCapacity { get; set; }
    }

    private sealed class Plane : Vehicle
    {
        public double Wingspan { get; set; }
    }
}
