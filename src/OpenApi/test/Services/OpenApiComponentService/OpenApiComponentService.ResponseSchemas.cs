// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;

public partial class OpenApiComponentServiceTests : OpenApiDocumentServiceTestBase
{
    public static object[][] ResponsesWithPrimitiveTypes =>
    [
        [() => 12, "application/json", "integer", "int32"],
        [() => Int64.MaxValue, "application/json", "integer", "int64"],
        [() => 12.0f, "application/json", "number", "float"],
        [() => 12.0, "application/json", "number", "double"],
        [() => 12.0m, "application/json", "number", "double"],
        [() => false, "application/json", "boolean", null],
        [() => "test", "text/plain", "string", null],
        [() => 't', "application/json", "string", null],
        [() => byte.MaxValue, "application/json", "string", "byte"],
        [() => short.MaxValue, "application/json", "integer", "int16"],
        [() => ushort.MaxValue, "application/json", "integer", "uint16"],
        [() => uint.MaxValue, "application/json", "integer", "uint32"],
        [() => ulong.MaxValue, "application/json", "integer", "uint64"],
        [() => new Uri("http://example.com"), "application/json", "string", "uri"]
    ];

    [Theory]
    [MemberData(nameof(ResponsesWithPrimitiveTypes))]
    public async Task GetOpenApiResponse_HandlesResponsesWithPrimitiveTypes(Delegate requestHandler, string contentType, string schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue(contentType, out var mediaType));
            Assert.Equal(schemaType, mediaType.Schema.Type);
            Assert.Equal(schemaFormat, mediaType.Schema.Format);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesPocoResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", () => new Todo(1, "Test Title", true, DateTime.Now));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
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
    public async Task GetOpenApiResponse_HandlesNullablePocoResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static Todo? GetTodo() => Random.Shared.Next() < 0.5 ? new Todo(1, "Test Title", true, DateTime.Now) : null;
        builder.MapGet("/api", GetTodo);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
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
    public async Task GetOpenApiResponse_HandlesInheritedTypeResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", () => new TodoWithDueDate(1, "Test Title", true, DateTime.Now, DateTime.Now.AddDays(1)));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("dueDate", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
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
    public async Task GetOpenApiResponse_HandlesGenericResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", () => new Result<Todo>(true, new TodoWithDueDate(1, "Test Title", true, DateTime.Now, DateTime.Now.AddDays(1)), null));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("isSuccessful", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("value", property.Key);
                    Assert.Equal("object", property.Value.Type);
                    Assert.Collection(property.Value.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                        Assert.Equal("int32", property.Value.Format);
                    }, property =>
                    {
                        Assert.Equal("title", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    }, property =>
                    {
                        Assert.Equal("completed", property.Key);
                        Assert.Equal("boolean", property.Value.Type);
                    }, property =>
                    {
                        Assert.Equal("createdAt", property.Key);
                        Assert.Equal("string", property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
                    });
                },
                property =>
                {
                    Assert.Equal("error", property.Key);
                    Assert.Equal("object", property.Value.Type);
                    Assert.Collection(property.Value.Properties, property =>
                    {
                        Assert.Equal("code", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                    }, property =>
                    {
                        Assert.Equal("message", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    });
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesPolymorphicResponseWithoutDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", () => new Boat { Length = 10, Make = "Type boat", Wheels = 0 });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
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

    [Fact]
    public async Task GetOpenApiResponse_HandlesResultOfAnonymousType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", () => TypedResults.Created("/test/1", new { Id = 1, Name = "Test", Todo = new Todo(1, "Test", true, DateTime.Now) }));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("object", mediaType.Schema.Type);
            Assert.Collection(mediaType.Schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("todo", property.Key);
                    Assert.Equal("object", property.Value.Type);
                    Assert.Collection(property.Value.Properties,
                        property =>
                        {
                            Assert.Equal("id", property.Key);
                            Assert.Equal("integer", property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
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
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesListOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => TypedResults.Ok<List<Todo>>([new Todo(1, "Test Title", true, DateTime.Now), new Todo(2, "Test Title 2", false, DateTime.Now)]));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            Assert.Equal("array", mediaType.Schema.Type);
            Assert.NotNull(mediaType.Schema.Items);
            Assert.Equal("object", mediaType.Schema.Items.Type);
            Assert.Collection(mediaType.Schema.Items.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
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
}
