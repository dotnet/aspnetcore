// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
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
        [() => 't', "application/json", "string", "char"],
        [() => byte.MaxValue, "application/json", "integer", "uint8"],
        [() => new byte[] { }, "application/json", "string", "byte"],
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
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
    public async Task GetOpenApiResponse_GeneratesSchemaForPoco_WithValidationAttributes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/", () => new ProjectBoard { Id = 2, Name = "Test", IsPrivate = false });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Get];
            var response = operation.Responses["200"];

            Assert.NotNull(response);
            var content = Assert.Single(response.Content);
            Assert.Equal("application/json", content.Key);
            Assert.NotNull(content.Value.Schema);
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal(1, property.Value.Minimum);
                    Assert.Equal(100, property.Value.Maximum);
                },
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal(5, property.Value.MinLength);
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
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
    public async Task GetOpenApiResponse_RespectsRequiredAttributeOnBodyProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/required-properties", () => new RequiredTodo { Title = "Test Title", Completed = true });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/required-properties"].Operations[OperationType.Post];
            var response = operation.Responses["200"];
            var content = Assert.Single(response.Content);
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Collection(schema.Required,
                property => Assert.Equal("title", property),
                property => Assert.Equal("completed", property));
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("dueDate", property.Key);
                    // DateTime schema appears twice in the document so we expect
                    // this to map to a reference ID.
                    var dateTimeSchema = property.Value.GetEffective(document);
                    Assert.Equal("string", dateTimeSchema.Type);
                    Assert.Equal("date-time", dateTimeSchema.Format);
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
                    // DateTime schema appears twice in the document so we expect
                    // this to map to a reference ID.
                    var dateTimeSchema = property.Value.GetEffective(document);
                    Assert.Equal("string", dateTimeSchema.Type);
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
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Empty(schema.AnyOf);
            Assert.Collection(schema.Properties,
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
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
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("array", schema.Type);
            Assert.NotNull(schema.Items);
            var effectiveItemsSchema = schema.Items.GetEffective(document);
            Assert.Equal("object", effectiveItemsSchema.Type);
            Assert.Collection(effectiveItemsSchema.Properties,
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
            var operation = document.Paths["/"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("pageIndex", property.Key);
                    Assert.Equal("integer", property.Value.GetEffective(document).Type);
                    Assert.Equal("int32", property.Value.GetEffective(document).Format);
                },
                property =>
                {
                    Assert.Equal("pageSize", property.Key);
                    Assert.Equal("integer", property.Value.GetEffective(document).Type);
                    Assert.Equal("int32", property.Value.GetEffective(document).Format);
                },
                property =>
                {
                    Assert.Equal("totalItems", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int64", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("totalPages", property.Key);
                    Assert.Equal("integer", property.Value.GetEffective(document).Type);
                    Assert.Equal("int32", property.Value.GetEffective(document).Format);
                },
                property =>
                {
                    Assert.Equal("items", property.Key);
                    Assert.Equal("array", property.Value.Type);
                    Assert.NotNull(property.Value.Items);
                    Assert.Equal("object", property.Value.Items.Type);
                    Assert.Collection(property.Value.Items.Properties,
                        property =>
                        {
                            Assert.Equal("id", property.Key);
                            Assert.Equal("integer", property.Value.GetEffective(document).Type);
                            Assert.Equal("int32", property.Value.GetEffective(document).Format);
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
            var operation = document.Paths["/"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/problem+json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("type", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("status", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("detail", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("instance", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("errors", property.Key);
                    Assert.Equal("object", property.Value.Type);
                    // The errors object is a dictionary of string[]. Use `additionalProperties`
                    // to indicate that the payload can be arbitrary keys with string[] values.
                    Assert.Equal("array", property.Value.AdditionalProperties.Type);
                    Assert.Equal("string", property.Value.AdditionalProperties.Items.GetEffective(document).Type);
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
            var operation = document.Paths["/"].Operations[OperationType.Get];
            var responses = Assert.Single(operation.Responses);
            var response = responses.Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("object", property.Key);
                    Assert.Null(property.Value.Type);
                    Assert.False(property.Value.Nullable);
                },
                property =>
                {
                    Assert.Equal("anotherObject", property.Key);
                    Assert.Null(property.Value.Type);
                    Assert.Equal(32, ((OpenApiInteger)property.Value.Default).Value);
                    Assert.Equal("This is a description", property.Value.Description);
                });
        });
    }

    private class ClassWithObjectProperty
    {
        public object Object { get; set; }

        [Description("This is a description")]
        [DefaultValue(32)]
        public object AnotherObject { get; set; }
    }
}
