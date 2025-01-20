// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

public class OpenApiSchemaReferenceTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task IdenticalParameterTypesAreStoredWithSchemaReference()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IFormFile value) => { });
        builder.MapPost("/api-2", (IFormFile value) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var parameter = operation.RequestBody.Content["multipart/form-data"];
            var schema = parameter.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var parameter2 = operation2.RequestBody.Content["multipart/form-data"];
            var schema2 = parameter2.Schema;

            // {
            //   "$ref": "#/components/schemas/IFormFileValue"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "IFormFileValue": {
            //         "type": "object",
            //         "properties": {
            //           "value": {
            //             "$ref": "#/components/schemas/IFormFile"
            //           }
            //         }
            //       },
            //       "IFormFile": {
            //         "type": "string",
            //         "format": "binary"
            //       }
            //     }
            //   }
            Assert.Equal(schema.Reference, schema2.Reference);

            var effectiveSchema = schema;
            Assert.Equal(JsonSchemaType.Object, effectiveSchema.Type);
            Assert.Single(effectiveSchema.Properties);
            var effectivePropertySchema = effectiveSchema.Properties["value"];
            Assert.Equal(JsonSchemaType.String, effectivePropertySchema.Type);
            Assert.Equal("binary", effectivePropertySchema.Format);
        });
    }

    [Fact]
    public async Task TodoInRequestBodyAndResponseUsesSchemaReference()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Todo todo) => TypedResults.Ok(todo));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var response = operation.Responses["200"];
            var responseContent = response.Content["application/json"];
            var responseSchema = responseContent.Schema;

            // {
            //   "$ref": "#/components/schemas/Todo"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "Todo": {
            //         "type": "object",
            //         "properties": {
            //           "id": {
            //             "type": "integer"
            //           },
            //           ...
            //         }
            //       }
            //     }
            //   }
            Assert.Equal(requestBodySchema.Reference.Id, responseSchema.Reference.Id);

            var effectiveSchema = requestBodySchema;
            Assert.Equal(JsonSchemaType.Object, effectiveSchema.Type);
            Assert.Equal(4, effectiveSchema.Properties.Count);
            var effectiveIdSchema = effectiveSchema.Properties["id"];
            Assert.Equal(JsonSchemaType.Integer, effectiveIdSchema.Type);
            var effectiveTitleSchema = effectiveSchema.Properties["title"];
            Assert.Equal(JsonSchemaType.String, effectiveTitleSchema.Type);
            var effectiveCompletedSchema = effectiveSchema.Properties["completed"];
            Assert.Equal(JsonSchemaType.Boolean, effectiveCompletedSchema.Type);
            var effectiveCreatedAtSchema = effectiveSchema.Properties["createdAt"];
            Assert.Equal(JsonSchemaType.String, effectiveCreatedAtSchema.Type);
        });
    }

    [Fact]
    public async Task SameTypeInDictionaryAndListTypesUsesReferenceIds()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Todo[] todo) => { });
        builder.MapPost("/api-2", (Dictionary<string, Todo> todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema;

            // {
            //   "type": "array",
            //   "items": {
            //     "$ref": "#/components/schemas/Todo"
            //   }
            // }
            // {
            //   "type": "object",
            //   "additionalProperties": {
            //     "$ref": "#/components/schemas/Todo"
            //   }
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "Todo": {
            //         "type": "object",
            //         "properties": {
            //           "id": {
            //             "type": "integer"
            //           },
            //           ...
            //         }
            //       }
            //     }
            //   }
            // }

            // Parent types of schemas are different
            Assert.Equal(JsonSchemaType.Array, requestBodySchema.Type);
            Assert.Equal(JsonSchemaType.Object, requestBodySchema2.Type);
            // Values of the list and dictionary point to the same reference ID
            Assert.Equal(requestBodySchema.Items.Reference.Id, requestBodySchema2.AdditionalProperties.Reference.Id);
        });
    }

    [Fact]
    public async Task SameTypeInAllOfReferenceGetsHandledCorrectly()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IFormFile resume, [FromForm] Todo todo) => { });
        builder.MapPost("/api-2", ([FromForm] string name, [FromForm] Todo todo2) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["multipart/form-data"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["multipart/form-data"];
            var requestBodySchema2 = requestBody2.Schema;

            // Todo parameter (second parameter) in allOf for each operation should point to the same reference ID.
            Assert.Equal(requestBodySchema.AllOf[1].Reference.Id, requestBodySchema2.AllOf[1].Reference.Id);

            // IFormFile parameter should use inline schema since it only appears once in the application.
            Assert.Equal(JsonSchemaType.Object, requestBodySchema.AllOf[0].Type);
            Assert.Equal(JsonSchemaType.String, requestBodySchema.AllOf[0].Properties["resume"].Type);
            Assert.Equal("binary", requestBodySchema.AllOf[0].Properties["resume"].Format);

            // string parameter is not resolved to a top-level reference.
            Assert.Equal(JsonSchemaType.Object, requestBodySchema2.AllOf[0].Type);
            Assert.Null(requestBodySchema.AllOf[1].Properties["title"].Reference);
            Assert.Null(requestBodySchema2.AllOf[1].Properties["title"].Reference);
        });
    }

    [Fact]
    public async Task DifferentTypesWithSameSchemaMapToSameReferenceId()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IEnumerable<Todo> todo) => { });
        builder.MapPost("/api-2", (Todo[] todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema;

            // {
            //  "type": "array",
            //  "items": {
            //    "$ref": "#/components/schemas/Todo"
            //  }
            // {
            //  "type": "array",
            //  "items": {
            //    "$ref": "#/components/schemas/Todo"
            //  }
            // {
            //   "components": {
            //     "schemas": {
            //       "TodoArray": {
            //         "type": "object",
            //         "properties": {
            //           ...
            //         }
            //       }
            //     }
            //   }
            // }

            // Both list types should be inlined
            Assert.Null(requestBodySchema.Reference);
            Assert.Equal(requestBodySchema.Reference, requestBodySchema2.Reference);
            // And have an `array` type
            Assert.Equal(JsonSchemaType.Array, requestBodySchema.Type);
            // With an `items` sub-schema should consist of a $ref to Todo
            Assert.Equal("Todo", requestBodySchema.Items.Reference.Id);
            Assert.Equal(requestBodySchema.Items.Reference.Id, requestBodySchema2.Items.Reference.Id);
            Assert.Equal(4, requestBodySchema.Items.Properties.Count);
        });
    }

    [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/58619")]
    public async Task TypeModifiedWithSchemaTransformerMapsToDifferentReferenceId()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Todo) && context.ParameterDescription is not null)
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny(context.ParameterDescription.Name);
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            // Schemas are distinct because of applied transformer so no reference is used.
            Assert.NotEqual(requestSchema.Reference.Id, responseSchema.Reference.Id);
            Assert.Equal("todo", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            Assert.False(responseSchema.Extensions.TryGetValue("x-my-extension", out var _));
        });
    }

    [Fact]
    public static async Task ProducesStableSchemaRefsForListOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", () => new TodoListContainer());
        builder.MapPost("/api-2", () => new TodoListContainer());
        builder.MapPost("/api-3", (Todo todo) => { });

        // Assert -- call twice to ensure the schema reference is stable
        await VerifyOpenApiDocument(builder, VerifyDocument);
        await VerifyOpenApiDocument(builder, VerifyDocument);

        static void VerifyDocument(OpenApiDocument document)
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.Responses["200"].Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.Responses["200"].Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema;

            // {
            //   "$ref": "#/components/schemas/TodoListContainer"
            // }
            // {
            //   "$ref": "#/components/schemas/TodoListContainer"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "TodoListContainer": {
            //         "properties": {
            //              "type": "array",
            //              "items": {
            //                  "$ref": "#/components/schemas/Todo"
            //              }
            //           }
            //       }
            //     }
            //   }
            // }

            // Both container types should point to the same reference ID
            Assert.Equal("TodoListContainer", requestBodySchema.Reference.Id);
            Assert.Equal(requestBodySchema.Reference.Id, requestBodySchema2.Reference.Id);
            // The referenced schema should have an array type with items pointing to Todo
            var effectiveSchema = requestBodySchema;
            var todosProperty = effectiveSchema.Properties["todos"];
            Assert.Equal(JsonSchemaType.Null | JsonSchemaType.Array, todosProperty.Type);
            var itemsSchema = todosProperty.Items;
            Assert.Equal("Todo", itemsSchema.Reference.Id);
            Assert.Equal(4, itemsSchema.Properties.Count);
        }
    }

    private class TodoListContainer
    {
        public ICollection<Todo> Todos { get; set; } = [];
    }

    [Fact]
    public async Task SupportsRefMappingInDeeplyNestedTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (Level1 item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("Level1", requestSchema.Reference.Id);

            // Assert that $ref is used for Level1.Item2
            var level1Schema = requestSchema;
            Assert.Equal("Level2", level1Schema.Properties["item2"].Reference.Id);

            // Assert that $ref is used for Level2.Item3
            var level2Schema = level1Schema.Properties["item2"];
            Assert.Equal("Level3", level2Schema.Properties["item3"].Reference.Id);

            // Assert that no $ref is used for string property
            var level3Schema = level2Schema.Properties["item3"];
            Assert.Null(level3Schema.Properties["terminate"].Reference);
        });
    }

    private class Level1
    {
        public Level2 Item2 { get; set; }
    }

    private class Level2
    {
        public Level3 Item3 { get; set; }
    }

    private class Level3
    {
        public string Terminate { get; set; }
    }

    [Fact]
    public async Task ThrowsForOverlyNestedSchemas()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (DeeplyNestedLevel1 item) => { });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => VerifyOpenApiDocument(builder, _ => { }));
        Assert.Equal("The depth of the generated JSON schema exceeds the JsonSerializerOptions.MaxDepth setting.", exception.Message);
    }

    [Fact]
    public async Task SupportsDeeplyNestedSchemaWithConfiguredMaxDepth()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.MaxDepth = 124;
        });
        var builder = CreateBuilder(serviceCollection);

        builder.MapPost("/", (DeeplyNestedLevel1 item) => { });

        // Act & Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("DeeplyNestedLevel1", requestSchema.Reference.Id);

            // Assert that $ref is used for all nested levels
            var levelSchema = requestSchema;
            for (var level = 2; level < 36; level++)
            {
                Assert.Equal($"DeeplyNestedLevel{level}", levelSchema.Properties[$"item{level}"].Reference.Id);
                levelSchema = levelSchema.Properties[$"item{level}"];
            }
        });
    }

    [Fact]
    public async Task SelfReferenceMapperOnlyOperatesOnSchemaReferenceTypes()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Todo))
            {
                schema.Reference = new OpenApiReference { Id = "#", Type = ReferenceType.Link };
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = document.Paths["/todo"].Operations[OperationType.Get];
            var response = operation.Responses["200"].Content["application/json"];
            var responseSchema = response.Schema;
            Assert.Equal("#", responseSchema.Reference.Id);
            Assert.Equal(ReferenceType.Link, responseSchema.Reference.Type);
        });
    }

    [Fact]
    public async Task SupportsNestedSchemasWithSelfReference()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (LocationContainer item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("LocationContainer", requestSchema.Reference.Id);

            // Assert that only expected schema references are generated
            Assert.Equal(3, document.Components.Schemas.Count);
            Assert.Collection(document.Components.Schemas.Keys,
                key => Assert.Equal("AddressDto", key),
                key => Assert.Equal("LocationContainer", key),
                key => Assert.Equal("LocationDto", key));

            // Assert that LocationContainer schema is serialized with correct refs
            var writer = new StringWriter();
            var openApiWriter = new OpenApiJsonWriter(writer);
            document.Components.Schemas["LocationContainer"].SerializeAsV31(openApiWriter);
            var serializedSchema = writer.ToString();
            Assert.Equal("""
            {
                "type": "object",
                "properties": {
                    "location": {
                        "$ref": "#/components/schemas/LocationDto"
                    }
                }
            }
            """, serializedSchema, ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);

            writer = new StringWriter();
            openApiWriter = new OpenApiJsonWriter(writer);
            document.Components.Schemas["LocationDto"].SerializeAsV31(openApiWriter);
            serializedSchema = writer.ToString();
            Assert.Equal("""
            {
                "type": [
                    "null",
                    "object"
                ],
                "properties": {
                    "address": {
                        "$ref": "#/components/schemas/AddressDto"
                    }
                }
            }
            """, serializedSchema, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);

            writer = new StringWriter();
            openApiWriter = new OpenApiJsonWriter(writer);
            document.Components.Schemas["AddressDto"].SerializeAsV31(openApiWriter);
            serializedSchema = writer.ToString();
            Assert.Equal("""
            {
                "type": [
                    "null",
                    "object"
                ],
                "properties": {
                    "relatedLocation": {
                        "$ref": "#/components/schemas/LocationDto"
                    }
                }
            }
            """, serializedSchema, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
        });
    }

    [Fact]
    public async Task SupportsListNestedSchemasWithSelfReference()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (ParentObject item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("ParentObject", requestSchema.Reference.Id);

            // Assert that only two schemas are generated
            Assert.Equal(2, document.Components.Schemas.Count);
            Assert.Collection(document.Components.Schemas.Keys,
                key => Assert.Equal("ChildObject", key),
                key => Assert.Equal("ParentObject", key));

            // Assert that ParentObject schema is serialized with correct refs
            var writer = new StringWriter();
            var openApiWriter = new OpenApiJsonWriter(writer);
            document.Components.Schemas["ParentObject"].SerializeAsV31(openApiWriter);
            var serializedSchema = writer.ToString();
            Assert.Equal("""
            {
                "type": "object",
                "properties": {
                "id": {
                    "type": "integer",
                    "format": "int32"
                },
                "children": {
                    "type": "array",
                    "items": {
                        "$ref": "#/components/schemas/ChildObject"
                    }
                }
                }
            }
            """, serializedSchema, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);

            writer = new StringWriter();
            openApiWriter = new OpenApiJsonWriter(writer);
            document.Components.Schemas["ChildObject"].SerializeAsV31(openApiWriter);
            serializedSchema = writer.ToString();
            Assert.Equal("""
            {
                "required": [
                    "parent"
                ],
                "type": "object",
                "properties": {
                    "id": {
                        "type": "integer",
                        "format": "int32"
                    },
                    "parent": {
                        "$ref": "#/components/schemas/ParentObject"
                    }
                }
            }
            """, serializedSchema, ignoreAllWhiteSpace: true, ignoreLineEndingDifferences: true);
        });
    }

    [Fact]
    public async Task SupportsMultiplePropertiesWithSameType()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (Root item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("Root", requestSchema.Reference.Id);

            // Assert that $ref is used for nested Item1
            Assert.Equal("Item", requestSchema.Properties["item1"].Reference.Id);

            // Assert that $ref is used for nested Item2
            Assert.Equal("Item", requestSchema.Properties["item2"].Reference.Id);
        });
    }

    private class Root
    {
        public Item Item1 { get; set; } = null!;
        public Item Item2 { get; set; } = null!;
    }

    private class Item
    {
        public string[] Name { get; set; } = null!;
        public int value { get; set; }
    }

    private class LocationContainer
    {

        public LocationDto Location { get; set; }
    }

    private class LocationDto
    {
        public AddressDto Address { get; set; }
    }

    private class AddressDto
    {
        public LocationDto RelatedLocation { get; set; }
    }

#nullable enable
    private class ParentObject
    {
        public int Id { get; set; }
        public List<ChildObject> Children { get; set; } = [];
    }

    private class ChildObject
    {
        public int Id { get; set; }
        public required ParentObject Parent { get; set; }
    }
}
#nullable restore
