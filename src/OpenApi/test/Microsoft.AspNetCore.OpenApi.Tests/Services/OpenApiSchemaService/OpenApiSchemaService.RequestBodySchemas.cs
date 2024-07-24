// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
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
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
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
            var effectiveSchema = content.Value.Schema.GetEffective(document);
            Assert.Equal("object", effectiveSchema.Type);
            Assert.Collection(effectiveSchema.Properties,
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
            var schema = content.Value.Schema.GetEffective(document);
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

                var effectiveSchema = requestBody.Content["application/octet-stream"].Schema.GetEffective(document);

                Assert.Equal("string", effectiveSchema.Type);
                Assert.Equal("binary", effectiveSchema.Format);
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
            Assert.Equal("Proposal", schema.Reference.Id);
            var effectiveSchema = schema.GetEffective(document);
            Assert.Collection(effectiveSchema.Properties,
                property => {
                    Assert.Equal("proposalElement", property.Key);
                    Assert.Equal("Proposal", property.Value.Reference.Id);
                },
                property => {
                    Assert.Equal("stream", property.Key);
                    var targetSchema = property.Value.GetEffective(document);
                    Assert.Equal("string", targetSchema.Type);
                    Assert.Equal("binary", targetSchema.Format);
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

            var enumerableTodoSchema = enumerableTodo.RequestBody.Content["application/json"].Schema;
            var arrayTodoSchema = arrayTodo.RequestBody.Content["application/json"].Schema;
            // Assert that both IEnumerable<Todo> and Todo[] have items that map to the same schema
            Assert.Equal(enumerableTodoSchema.Items.Reference.Id, arrayTodoSchema.Items.Reference.Id);
            // Assert all types materialize as arrays
            Assert.Equal("array", enumerableTodoSchema.Type);
            Assert.Equal("array", arrayTodoSchema.Type);

            Assert.Equal("array", parameter.Schema.Type);
            Assert.Equal("string", parameter.Schema.Items.Type);
            Assert.Equal("uuid", parameter.Schema.Items.Format);

            // Assert the array items are the same as the Todo schema
            foreach (var element in new[] { enumerableTodoSchema, arrayTodoSchema })
            {
                Assert.Collection(element.Items.GetEffective(document).Properties,
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
    public async Task GetOpenApiRequestBody_HandlesDescriptionAttributeOnProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (DescriptionTodo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                    Assert.Equal("The unique identifier for a todo item.", property.Value.Description);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("The title of the todo item.", property.Value.Description);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                    Assert.Equal("The completion status of the todo item.", property.Value.Description);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                    Assert.Equal("The date and time the todo item was created.", property.Value.Description);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesDescriptionAttributeOnParameter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", ([Description("The todo item to create.")] DescriptionTodo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.Equal("The todo item to create.", operation.RequestBody.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesNullableProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NullablePropertiesType type) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("nullableInt", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.True(property.Value.Nullable);
                },
                property =>
                {
                    Assert.Equal("nullableString", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.True(property.Value.Nullable);
                },
                property =>
                {
                    Assert.Equal("nullableBool", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                    Assert.True(property.Value.Nullable);
                },
                property =>
                {
                    Assert.Equal("nullableDateTime", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                    Assert.True(property.Value.Nullable);
                },
                property =>
                {
                    Assert.Equal("nullableUri", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("uri", property.Value.Format);
                    Assert.True(property.Value.Nullable);
                });
        });
    }

    [Fact]
    public async Task SupportsNestedTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (NestedType type) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("NestedType", content.Value.Schema.Reference.Id);
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("nested", property.Key);
                    Assert.Equal("NestedType", property.Value.Reference.Id);
                });
        });
    }

    [Fact]
    public async Task SupportsNestedTypes_WithNoAttributeProvider()
    {
        // Arrange: this test ensures that we can correctly handle the scenario
        // where the attribute provider is null and we need to patch the property mappings
        // that are created by the underlying JsonSchemaExporter.
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolver = options.SerializerOptions.TypeInfoResolver?.WithAddedModifier(jsonTypeInfo =>
            {
                foreach (var propertyInfo in jsonTypeInfo.Properties)
                {
                    propertyInfo.AttributeProvider = null;
                }

            });
        });
        var builder = CreateBuilder(serviceCollection);

        // Act
        builder.MapPost("/api", (NestedType type) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("NestedType", content.Value.Schema.Reference.Id);
            var schema = content.Value.Schema.GetEffective(document);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("name", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("nested", property.Key);
                    Assert.Equal("NestedType", property.Value.Reference.Id);
                });
        });
    }

    private class DescriptionTodo
    {
        [Description("The unique identifier for a todo item.")]
        public int Id { get; set; }

        [Description("The title of the todo item.")]
        public string Title { get; set; }

        [Description("The completion status of the todo item.")]
        public bool Completed { get; set; }

        [Description("The date and time the todo item was created.")]
        public DateTime CreatedAt { get; set; }
    }

#nullable enable
    private class NullablePropertiesType
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
        public bool? NullableBool { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public Uri? NullableUri { get; set; }
    }
#nullable restore

    private class NestedType
    {
        public string Name { get; set; }
        public NestedType Nested { get; set; }
    }
}
