// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    public static object[][] ResponsesWithPrimitiveTypes =>
    [
        [() => 12, "application/json", JsonSchemaType.Integer, "int32"],
        [() => Int64.MaxValue, "application/json", JsonSchemaType.Integer, "int64"],
        [() => 12.0f, "application/json", JsonSchemaType.Number, "float"],
        [() => 12.0, "application/json", JsonSchemaType.Number, "double"],
        [() => 12.0m, "application/json", JsonSchemaType.Number, "double"],
        [() => false, "application/json", JsonSchemaType.Boolean, null],
        [() => "test", "text/plain", JsonSchemaType.String, null],
        [() => 't', "application/json", JsonSchemaType.String, "char"],
        [() => byte.MaxValue, "application/json", JsonSchemaType.Integer, "uint8"],
        [() => new byte[] { }, "application/json", JsonSchemaType.String, "byte"],
        [() => short.MaxValue, "application/json", JsonSchemaType.Integer, "int16"],
        [() => ushort.MaxValue, "application/json", JsonSchemaType.Integer, "uint16"],
        [() => uint.MaxValue, "application/json", JsonSchemaType.Integer, "uint32"],
        [() => ulong.MaxValue, "application/json", JsonSchemaType.Integer, "uint64"],
        [() => new Uri("http://example.com"), "application/json", JsonSchemaType.String, "uri"]
    ];

    [Theory]
    [MemberData(nameof(ResponsesWithPrimitiveTypes))]
    public async Task GetOpenApiResponse_HandlesResponsesWithPrimitiveTypes(Delegate requestHandler, string contentType, JsonSchemaType schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_GeneratesSchemaForPoco_WithValidationAttributes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => new ProjectBoard { Id = 2, Name = "Test", IsPrivate = false });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var response = operation.Responses["200"];

            Assert.NotNull(response);
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            var schema = content.Value.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("1", property.Value.Minimum);
                    Assert.Equal("100", property.Value.Maximum);
                },
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                    Assert.Equal(5, property.Value.MinLength);
                    Assert.Equal(10, property.Value.MaxLength);
                    Assert.Null(property.Value.Default);
                },
                property =>
                {
                    Assert.Equal("description", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                    Assert.Equal(5, property.Value.MinLength);
                    Assert.Equal(10, property.Value.MaxLength);
                },
                property =>
                {
                    Assert.Equal("isPrivate", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                    Assert.True(property.Value.Default.GetValue<bool>());
                },
                property =>
                {
                    Assert.Equal("items", property.Key);
                    Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, property.Value.Type);
                    Assert.Equal(10, property.Value.MaxItems);
                },
                property =>
                {
                    Assert.Equal("tags", property.Key);
                    Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, property.Value.Type);
                    Assert.Equal(5, property.Value.MinItems);
                    Assert.Equal(10, property.Value.MaxItems);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.NotNull(schema.OneOf);
            Assert.Equal(2, schema.OneOf.Count);
            // Check that the oneOf consists of a nullable schema and the GetTodo schema
            Assert.Collection(schema.OneOf,
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Null, item.Type);
                },
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Object, item.Type);
                    Assert.Collection(item.Properties,
                        property =>
                        {
                            Assert.Equal("id", property.Key);
                            Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
                        },
                        property =>
                        {
                            Assert.Equal("title", property.Key);
                            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("completed", property.Key);
                            Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("createdAt", property.Key);
                            Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            Assert.Equal("date-time", property.Value.Format);
                        });
                });

        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesNullablePocoTaskResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static Task<Todo?> GetTodoAsync() => Task.FromResult(Random.Shared.Next() < 0.5 ? new Todo(1, "Test Title", true, DateTime.Now) : null);
        builder.MapGet("/api", GetTodoAsync);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesNullablePocoValueTaskResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static ValueTask<Todo?> GetTodoValueTaskAsync() => ValueTask.FromResult(Random.Shared.Next() < 0.5 ? new Todo(1, "Test Title", true, DateTime.Now) : null);
        builder.MapGet("/api", GetTodoValueTaskAsync);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_RespectsRequiredAttributeOnBodyProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/required-properties", () => new RequiredTodo { Title = "Test Title", Completed = true });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/required-properties"].Operations[HttpMethod.Post];
            var response = operation.Responses["200"];
            var content = Assert.Single(response.Content);
            var schema = content.Value.Schema;
            Assert.Collection(schema.Required,
                property => Assert.Equal("title", property),
                property => Assert.Equal("completed", property));
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesNullableValueTypeResponse()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static Point? GetNullablePoint() => Random.Shared.Next() < 0.5 ? new Point { X = 10, Y = 20 } : null;
        builder.MapGet("/api/nullable-point", GetNullablePoint);

        static Coordinate? GetNullableCoordinate() => Random.Shared.Next() < 0.5 ? new Coordinate(1.5, 2.5) : null;
        builder.MapGet("/api/nullable-coordinate", GetNullableCoordinate);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            // Verify nullable Point response
            var pointOperation = document.Paths["/api/nullable-point"].Operations[HttpMethod.Get];
            var pointResponses = Assert.Single(pointOperation.Responses);
            var pointResponse = pointResponses.Value;
            Assert.True(pointResponse.Content.TryGetValue("application/json", out var pointMediaType));
            var pointSchema = pointMediaType.Schema;
            Assert.NotNull(pointSchema.OneOf);
            Assert.Equal(2, pointSchema.OneOf.Count);
            Assert.Collection(pointSchema.OneOf,
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Null, item.Type);
                },
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Object, item.Type);
                    Assert.Collection(item.Properties,
                        property =>
                        {
                            Assert.Equal("x", property.Key);
                            Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
                        },
                        property =>
                        {
                            Assert.Equal("y", property.Key);
                            Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
                        });
                });

            // Verify nullable Coordinate response
            var coordinateOperation = document.Paths["/api/nullable-coordinate"].Operations[HttpMethod.Get];
            var coordinateResponses = Assert.Single(coordinateOperation.Responses);
            var coordinateResponse = coordinateResponses.Value;
            Assert.True(coordinateResponse.Content.TryGetValue("application/json", out var coordinateMediaType));
            var coordinateSchema = coordinateMediaType.Schema;
            Assert.NotNull(coordinateSchema.OneOf);
            Assert.Equal(2, coordinateSchema.OneOf.Count);
            Assert.Collection(coordinateSchema.OneOf,
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Null, item.Type);
                },
                item =>
                {
                    Assert.NotNull(item);
                    Assert.Equal(JsonSchemaType.Object, item.Type);
                    Assert.Collection(item.Properties,
                        property =>
                        {
                            Assert.Equal("latitude", property.Key);
                            Assert.Equal(JsonSchemaType.Number, property.Value.Type);
                            Assert.Equal("double", property.Value.Format);
                        },
                        property =>
                        {
                            Assert.Equal("longitude", property.Key);
                            Assert.Equal(JsonSchemaType.Number, property.Value.Type);
                            Assert.Equal("double", property.Value.Format);
                        });
                });

            // Assert that Point and Coordinates are the only schemas defined at the top-level
            Assert.Equal(["Coordinate", "Point"], [.. document.Components.Schemas.Keys]);
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesNullableCollectionResponsesWithOneOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static List<Todo>? GetNullableTodos() => Random.Shared.Next() < 0.5 ?
            [new Todo(1, "Test", true, DateTime.Now)] : null;
        static Todo[]? GetNullableTodoArray() => Random.Shared.Next() < 0.5 ?
            [new Todo(1, "Test", true, DateTime.Now)] : null;
        static IEnumerable<Todo>? GetNullableTodoEnumerable() => Random.Shared.Next() < 0.5 ?
            [new Todo(1, "Test", true, DateTime.Now)] : null;

        builder.MapGet("/api/nullable-list", GetNullableTodos);
        builder.MapGet("/api/nullable-array", GetNullableTodoArray);
        builder.MapGet("/api/nullable-enumerable", GetNullableTodoEnumerable);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            // Verify nullable List<Todo> response uses oneOf
            var listOperation = document.Paths["/api/nullable-list"].Operations[HttpMethod.Get];
            var listResponse = Assert.Single(listOperation.Responses).Value;
            Assert.True(listResponse.Content.TryGetValue("application/json", out var listMediaType));
            var listSchema = listMediaType.Schema;
            Assert.NotNull(listSchema.OneOf);
            Assert.Equal(2, listSchema.OneOf.Count);
            Assert.Collection(listSchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item =>
                {
                    Assert.Equal(JsonSchemaType.Array, item.Type);
                    Assert.NotNull(item.Items);
                    Assert.Equal("Todo", ((OpenApiSchemaReference)item.Items).Reference.Id);
                });

            // Verify nullable Todo[] response uses oneOf
            var arrayOperation = document.Paths["/api/nullable-array"].Operations[HttpMethod.Get];
            var arrayResponse = Assert.Single(arrayOperation.Responses).Value;
            Assert.True(arrayResponse.Content.TryGetValue("application/json", out var arrayMediaType));
            var arraySchema = arrayMediaType.Schema;
            Assert.NotNull(arraySchema.OneOf);
            Assert.Equal(2, arraySchema.OneOf.Count);
            Assert.Collection(arraySchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item =>
                {
                    Assert.Equal(JsonSchemaType.Array, item.Type);
                    Assert.NotNull(item.Items);
                    Assert.Equal("Todo", ((OpenApiSchemaReference)item.Items).Reference.Id);
                });

            // Verify nullable IEnumerable<Todo> response uses oneOf
            var enumerableOperation = document.Paths["/api/nullable-enumerable"].Operations[HttpMethod.Get];
            var enumerableResponse = Assert.Single(enumerableOperation.Responses).Value;
            Assert.True(enumerableResponse.Content.TryGetValue("application/json", out var enumerableMediaType));
            var enumerableSchema = enumerableMediaType.Schema;
            Assert.NotNull(enumerableSchema.OneOf);
            Assert.Equal(2, enumerableSchema.OneOf.Count);
            Assert.Collection(enumerableSchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item =>
                {
                    Assert.Equal(JsonSchemaType.Array, item.Type);
                    Assert.NotNull(item.Items);
                    Assert.Equal("Todo", ((OpenApiSchemaReference)item.Items).Reference.Id);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesNullableEnumResponsesWithOneOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#nullable enable
        static Status? GetNullableStatus() => Random.Shared.Next() < 0.5 ? Status.Approved : null;
        static TaskStatus? GetNullableTaskStatus() => Random.Shared.Next() < 0.5 ? TaskStatus.Running : null;

        builder.MapGet("/api/nullable-status", GetNullableStatus);
        builder.MapGet("/api/nullable-task-status", GetNullableTaskStatus);
#nullable restore

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            // Verify nullable Status (with string converter) response uses oneOf
            var statusOperation = document.Paths["/api/nullable-status"].Operations[HttpMethod.Get];
            var statusResponse = Assert.Single(statusOperation.Responses).Value;
            Assert.True(statusResponse.Content.TryGetValue("application/json", out var statusMediaType));
            var statusSchema = statusMediaType.Schema;
            Assert.NotNull(statusSchema.OneOf);
            Assert.Equal(2, statusSchema.OneOf.Count);
            Assert.Collection(statusSchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item =>
                {
                    // Status has string enum converter, so it should be a reference to the enum schema
                    Assert.Equal("Status", ((OpenApiSchemaReference)item).Reference.Id);
                });

            // Verify nullable TaskStatus (without converter) response uses oneOf
            var taskStatusOperation = document.Paths["/api/nullable-task-status"].Operations[HttpMethod.Get];
            var taskStatusResponse = Assert.Single(taskStatusOperation.Responses).Value;
            Assert.True(taskStatusResponse.Content.TryGetValue("application/json", out var taskStatusMediaType));
            var taskStatusSchema = taskStatusMediaType.Schema;
            Assert.NotNull(taskStatusSchema.OneOf);
            Assert.Equal(2, taskStatusSchema.OneOf.Count);
            Assert.Collection(taskStatusSchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal(JsonSchemaType.Integer, item.Type));
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("dueDate", property.Key);
                    // DateTime schema appears twice in the document so we expect
                    // this to map to a reference ID.
                    var dateTimeSchema = property.Value;
                    Assert.Equal(JsonSchemaType.String, dateTimeSchema.Type);
                    Assert.Equal("date-time", dateTimeSchema.Format);
                },
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    var dateTimeSchema = property.Value;
                    Assert.Equal(JsonSchemaType.String, dateTimeSchema.Type);
                    Assert.Equal("date-time", dateTimeSchema.Format);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("isSuccessful", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("value", property.Key);
                    var propertyValue = property.Value;
                    Assert.Equal(JsonSchemaType.Object, propertyValue.Type);
                    Assert.Collection(propertyValue.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                        Assert.Equal("int32", property.Value.Format);
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
                },
                property =>
                {
                    Assert.Equal("error", property.Key);
                    var propertyValue = property.Value;
                    Assert.Equal(JsonSchemaType.Object, propertyValue.Type);
                    Assert.Collection(propertyValue.Properties, property =>
                    {
                        Assert.Equal("code", property.Key);
                        Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    }, property =>
                    {
                        Assert.Equal("message", property.Key);
                        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Null(schema.AnyOf);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("length", property.Key);
                    Assert.Equal(JsonSchemaType.Number, property.Value.Type);
                    Assert.Equal("double", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("wheels", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("make", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("todo", property.Key);
                    var propertyValue = property.Value;
                    Assert.Equal(JsonSchemaType.Object, propertyValue.Type);
                    Assert.Collection(propertyValue.Properties,
                        property =>
                        {
                            Assert.Equal("id", property.Key);
                            Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
                        },
                        property =>
                        {
                            Assert.Equal("title", property.Key);
                            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("completed", property.Key);
                            Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("createdAt", property.Key);
                            Assert.Equal(JsonSchemaType.String, property.Value.Type);
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
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Array, schema.Type);
            Assert.NotNull(schema.Items);
            var effectiveItemsSchema = schema.Items;
            Assert.Equal(JsonSchemaType.Object, effectiveItemsSchema.Type);
            Assert.Collection(effectiveItemsSchema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesGenericType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => TypedResults.Ok<PaginatedItems<Todo>>(new(0, 1, 5, 50, [new Todo(1, "Test Title", true, DateTime.Now), new Todo(2, "Test Title 2", false, DateTime.Now)])));

        // Assert that the response schema is correctly generated. For now, generics are inlined
        // in the generated OpenAPI schema since OpenAPI supports generics via dynamic references as of
        // OpenAPI 3.1.0.
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("pageIndex", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("pageSize", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("totalItems", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int64", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("totalPages", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("items", property.Key);
                    Assert.Equal(JsonSchemaType.Null | JsonSchemaType.Array, property.Value.Type);
                    Assert.NotNull(property.Value.Items);
                    Assert.Equal(JsonSchemaType.Object, property.Value.Items.Type);
                    var itemsValue = property.Value.Items;
                    Assert.Collection(itemsValue.Properties,
                        property =>
                        {
                            Assert.Equal("id", property.Key);
                            Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                            Assert.Equal("int32", property.Value.Format);
                        },
                        property =>
                        {
                            Assert.Equal("title", property.Key);
                            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("completed", property.Key);
                            Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                        },
                        property =>
                        {
                            Assert.Equal("createdAt", property.Key);
                            Assert.Equal(JsonSchemaType.String, property.Value.Type);
                            Assert.Equal("date-time", property.Value.Format);
                        });
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_HandlesValidationProblem()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"]
        }));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/problem+json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("type", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("status", property.Key);
                    Assert.Equal(JsonSchemaType.Integer | JsonSchemaType.Null, property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("detail", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("instance", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("errors", property.Key);
                    Assert.Equal(JsonSchemaType.Object, property.Value.Type);
                    // The errors object is a dictionary of string[]. Use `additionalProperties`
                    // to indicate that the payload can be arbitrary keys with string[] values.
                    Assert.Equal(JsonSchemaType.Array, property.Value.AdditionalProperties.Type);
                    Assert.Equal(JsonSchemaType.String, property.Value.AdditionalProperties.Items.Type);
                });
        });
    }

    // Test for https://github.com/dotnet/aspnetcore/issues/56351
    [Fact]
    public async Task GetOpenApiResponse_SupportsObjectTypeProperty()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => new ClassWithObjectProperty { Object = new Todo(1, "Test Title", true, DateTime.Now) });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("object", property.Key);
                    Assert.Null(property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("anotherObject", property.Key);
                    Assert.Null(property.Value.Type);
                    var defaultValue = Assert.IsAssignableFrom<JsonNode>(property.Value.Default);
                    Assert.Equal(32, defaultValue.GetValue<int>());
                    Assert.Equal("This is a description", property.Value.Description);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiResponse_SupportsProducesWithProducesResponseTypeOnController()
    {
        var actionDescriptor = CreateActionDescriptor(nameof(TestController.Get), typeof(TestController));

        await VerifyOpenApiDocument(actionDescriptor, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal(JsonSchemaType.Integer, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal(JsonSchemaType.Boolean, property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal(JsonSchemaType.String, property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });
        });
    }

    [ApiController]
    [Produces("application/json")]
    public class TestController
    {
        [Route("/")]
        [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
        internal Todo Get() => new(1, "Write test", false, DateTime.Now);
    }

    private class ClassWithObjectProperty
    {
        public object Object { get; set; }

        [Description("This is a description")]
        [DefaultValue(32)]
        public object AnotherObject { get; set; }
    }

    private struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private readonly struct Coordinate
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
