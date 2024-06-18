// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public partial class OpenApiComponentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForPoco()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (Todo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;

            Assert.NotNull(requestBody);
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.Collection(content.Value.Schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });

        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForPoco_WithValidationAttributes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (ProjectBoard todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;

            Assert.NotNull(requestBody);
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.Collection(content.Value.Schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal(1, property.Value.Minimum);
                    Assert.Equal(100, property.Value.Maximum);
                    Assert.True(property.Value.Default is OpenApiNull);
                },
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal(5, property.Value.MinLength);
                    Assert.True(property.Value.Default is OpenApiNull);
                },
                property =>
                {
                    Assert.Equal("isPrivate", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                    var defaultValue = Assert.IsAssignableFrom<OpenApiBoolean>(property.Value.Default);
                    Assert.True(defaultValue.Value);
                });

        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_RespectsRequiredAttributeOnBodyParameter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/required-poco", ([Required] Todo todo) => { });
        builder.MapPost("/non-required-poco", (Todo todo) => { });
        builder.MapPost("/required-form", ([Required][FromForm] Todo todo) => { });
        builder.MapPost("/non-required-form", ([FromForm] Todo todo) => { });
        builder.MapPost("/", (ProjectBoard todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.True(GetRequestBodyForPath(document, "/required-poco").Required);
            Assert.False(GetRequestBodyForPath(document, "/non-required-poco").Required);
            Assert.True(GetRequestBodyForPath(document, "/required-form").Required);
            Assert.False(GetRequestBodyForPath(document, "/non-required-form").Required);
        });

        static OpenApiRequestBody GetRequestBodyForPath(OpenApiDocument document, string path)
        {
            var operation = document.Paths[path].Operations[OperationType.Post];
            return operation.RequestBody;
        }
    }

    [Fact]
    public async Task GetOpenApiRequestBody_RespectsRequiredAttributeOnBodyProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/required-properties", (RequiredTodo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/required-properties"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema;
            Assert.Collection(schema.Required,
                property => Assert.Equal("title", property),
                property => Assert.Equal("completed", property));
            Assert.DoesNotContain("assignee", schema.Required);
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForFileTypes()
    {
        // Arrange
        var builder = CreateBuilder();
        string[] paths = ["stream", "pipereader"];

        // Act
        builder.MapPost("/stream", ([FromBody] Stream stream) => { });
        builder.MapPost("/pipereader", ([FromBody] PipeReader stream) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            foreach (var path in paths)
            {
                var operation = document.Paths[$"/{path}"].Operations[OperationType.Post];
                var requestBody = operation.RequestBody;

                Assert.Equal("string", requestBody.Content["application/octet-stream"].Schema.Type);
                Assert.Equal("binary", requestBody.Content["application/octet-stream"].Schema.Format);
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForFilesInRecursiveType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/proposal", (Proposal stream) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths[$"/proposal"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var schema = requestBody.Content["application/json"].Schema;
            Assert.Collection(schema.Properties,
                property => {
                    Assert.Equal("proposalElement", property.Key);
                    // Todo: Assert that refs are used correctly.
                },
                property => {
                    Assert.Equal("stream", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("binary", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForListOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/enumerable-todo", (IEnumerable<Todo> todo) => { });
        builder.MapPost("/array-todo", (Todo[] todo) => { });
        builder.MapGet("/array-parsable", (Guid[] guids) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var enumerableTodo = document.Paths["/enumerable-todo"].Operations[OperationType.Post];
            var arrayTodo = document.Paths["/array-todo"].Operations[OperationType.Post];
            var arrayParsable = document.Paths["/array-parsable"].Operations[OperationType.Get];

            Assert.NotNull(enumerableTodo.RequestBody);
            Assert.NotNull(arrayTodo.RequestBody);
            var parameter = Assert.Single(arrayParsable.Parameters);

            // Assert all types materialize as arrays
            Assert.Equal("array", enumerableTodo.RequestBody.Content["application/json"].Schema.Type);
            Assert.Equal("array", arrayTodo.RequestBody.Content["application/json"].Schema.Type);

            Assert.Equal("array", parameter.Schema.Type);
            Assert.Equal("string", parameter.Schema.Items.Type);
            Assert.Equal("uuid", parameter.Schema.Items.Format);

            // Assert the array items are the same as the Todo schema
            foreach (var element in new[] { enumerableTodo, arrayTodo })
            {
                Assert.Collection(element.RequestBody.Content["application/json"].Schema.Items.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("title", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("completed", property.Key);
                        Assert.Equal("boolean", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("createdAt", property.Key);
                        Assert.Equal("string", property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
                    });
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesPolymorphicRequestWithoutDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Boat boat) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Empty(mediaType.Schema.AnyOf);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("length", property.Key);
                    Assert.Equal("number", property.Value.Type);
                    Assert.Equal("double", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("wheels", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("make", property.Key);
                    Assert.Equal("string", property.Value.Type);
                });
        });
    }
}
