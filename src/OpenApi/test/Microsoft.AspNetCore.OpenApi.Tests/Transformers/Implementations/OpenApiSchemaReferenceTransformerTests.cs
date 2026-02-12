// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var parameter = operation.RequestBody.Content["multipart/form-data"];
            var schema = parameter.Schema;

            var operation2 = document.Paths["/api-2"].Operations[HttpMethod.Post];
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
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            var value = Assert.Single(schema.Properties).Value;
            Assert.Equal("IFormFile", ((OpenApiSchemaReference)value).Reference.Id);

            Assert.Equal(JsonSchemaType.Object, schema2.Type);
            var value2 = Assert.Single(schema2.Properties).Value;
            Assert.Equal("IFormFile", ((OpenApiSchemaReference)value2).Reference.Id);

            var effectiveSchema = ((OpenApiSchemaReference)value).Target;
            Assert.Equal(JsonSchemaType.String, effectiveSchema.Type);
            Assert.Equal("binary", effectiveSchema.Format);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
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
            // }
            Assert.Equal(((OpenApiSchemaReference)requestBodySchema).Reference.Id, ((OpenApiSchemaReference)responseSchema).Reference.Id);

            var effectiveSchema = requestBodySchema;
            Assert.Equal(JsonSchemaType.Object, effectiveSchema.Type);
            Assert.Equal(4, effectiveSchema.Properties.Count);
            var effectiveIdSchema = effectiveSchema.Properties["id"];
            Assert.Equal(JsonSchemaType.Integer, effectiveIdSchema.Type);
            var effectiveTitleSchema = effectiveSchema.Properties["title"];
            Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, effectiveTitleSchema.Type);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[HttpMethod.Post];
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
            Assert.Equal(((OpenApiSchemaReference)requestBodySchema.Items).Reference.Id, ((OpenApiSchemaReference)requestBodySchema2.AdditionalProperties).Reference.Id);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody.Content["multipart/form-data"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[HttpMethod.Post];
            var requestBody2 = operation2.RequestBody.Content["multipart/form-data"];
            var requestBodySchema2 = requestBody2.Schema;

            // Todo parameter (second parameter) in allOf for each operation should point to the same reference ID.
            Assert.Equal(((OpenApiSchemaReference)requestBodySchema.AllOf[1]).Reference.Id, ((OpenApiSchemaReference)requestBodySchema2.AllOf[1]).Reference.Id);

            // IFormFile parameter should use inline schema since it only appears once in the application.
            Assert.Equal(JsonSchemaType.Object, requestBodySchema.AllOf[0].Type);
            Assert.Equal(JsonSchemaType.String, requestBodySchema.AllOf[0].Properties["resume"].Type);
            Assert.Equal("binary", requestBodySchema.AllOf[0].Properties["resume"].Format);

            // string parameter is not resolved to a top-level reference.
            Assert.Equal(JsonSchemaType.Object, requestBodySchema2.AllOf[0].Type);
            Assert.IsNotType<OpenApiSchemaReference>(requestBodySchema.AllOf[1].Properties["title"]);
            Assert.IsNotType<OpenApiSchemaReference>(requestBodySchema2.AllOf[1].Properties["title"]);
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[HttpMethod.Post];
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
            Assert.IsNotType<OpenApiSchemaReference>(requestBodySchema);
            Assert.IsNotType<OpenApiSchemaReference>(requestBodySchema2);
            // And have an `array` type
            Assert.Equal(JsonSchemaType.Array, requestBodySchema.Type);
            // With an `items` sub-schema should consist of a $ref to Todo
            Assert.Equal("Todo", ((OpenApiSchemaReference)requestBodySchema.Items).Reference.Id);
            Assert.Equal(((OpenApiSchemaReference)requestBodySchema.Items).Reference.Id, ((OpenApiSchemaReference)requestBodySchema2.Items).Reference.Id);
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
                schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                schema.Extensions["x-my-extension"] = new JsonNodeExtension(context.ParameterDescription.Name);
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[HttpMethod.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            var getOperation = path.Operations[HttpMethod.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            // Schemas are distinct because of applied transformer so no reference is used.
            Assert.NotEqual(((OpenApiSchemaReference)requestSchema).Reference.Id, ((OpenApiSchemaReference)responseSchema).Reference.Id);
            Assert.Equal("todo", ((JsonNodeExtension)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
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
            var operation = document.Paths["/api"].Operations[HttpMethod.Post];
            var requestBody = operation.Responses["200"].Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[HttpMethod.Post];
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
            Assert.Equal("TodoListContainer", ((OpenApiSchemaReference)requestBodySchema).Reference.Id);
            Assert.Equal(((OpenApiSchemaReference)requestBodySchema).Reference.Id, ((OpenApiSchemaReference)requestBodySchema2).Reference.Id);
            // The referenced schema should have an array type with items pointing to Todo
            var effectiveSchema = requestBodySchema;
            var todosProperty = effectiveSchema.Properties["todos"];
            Assert.Equal(JsonSchemaType.Null | JsonSchemaType.Array, todosProperty.Type);
            var itemsSchema = todosProperty.Items;
            Assert.Equal("Todo", ((OpenApiSchemaReference)itemsSchema).Reference.Id);
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
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("Level1", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for Level1.Item2
            var level1Schema = requestSchema;
            Assert.Equal("Level2", ((OpenApiSchemaReference)level1Schema.Properties["item2"]).Reference.Id);

            // Assert that $ref is used for Level2.Item3
            var level2Schema = level1Schema.Properties["item2"];
            Assert.Equal("Level3", ((OpenApiSchemaReference)level2Schema.Properties["item3"]).Reference.Id);

            // Assert that no $ref is used for string property
            var level3Schema = level2Schema.Properties["item3"];
            Assert.IsNotType<OpenApiSchemaReference>(level3Schema.Properties["terminate"]);
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
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("DeeplyNestedLevel1", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for all nested levels
            var levelSchema = requestSchema;
            for (var level = 2; level < 36; level++)
            {
                Assert.Equal($"DeeplyNestedLevel{level}", ((OpenApiSchemaReference)levelSchema.Properties[$"item{level}"]).Reference.Id);
                levelSchema = levelSchema.Properties[$"item{level}"];
            }
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
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("LocationContainer", ((OpenApiSchemaReference)requestSchema).Reference.Id);

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
                "type": "object",
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
                "type": "object",
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
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)requestSchema).Reference.Id);

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
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("Root", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for nested Item1
            Assert.Equal("Item", ((OpenApiSchemaReference)requestSchema.Properties["item1"]).Reference.Id);

            // Assert that $ref is used for nested Item2
            Assert.Equal("Item", ((OpenApiSchemaReference)requestSchema.Properties["item2"]).Reference.Id);
        });
    }

    [Fact]
    public async Task SupportsListOfNestedSchemasWithSelfReference()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/list", (List<LocationContainer> items) => { });
        builder.MapPost("/array", (LocationContainer[] items) => { });
        builder.MapPost("/dictionary", (Dictionary<string, LocationContainer> items) => { });
        builder.MapPost("/", (LocationContainer item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var listOperation = document.Paths["/list"].Operations[HttpMethod.Post];
            var listRequestSchema = listOperation.RequestBody.Content["application/json"].Schema;

            var arrayOperation = document.Paths["/array"].Operations[HttpMethod.Post];
            var arrayRequestSchema = arrayOperation.RequestBody.Content["application/json"].Schema;

            var dictionaryOperation = document.Paths["/dictionary"].Operations[HttpMethod.Post];
            var dictionaryRequestSchema = dictionaryOperation.RequestBody.Content["application/json"].Schema;

            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("LocationContainer", ((OpenApiSchemaReference)listRequestSchema.Items).Reference.Id);
            Assert.Equal("LocationContainer", ((OpenApiSchemaReference)arrayRequestSchema.Items).Reference.Id);
            Assert.Equal("LocationContainer", ((OpenApiSchemaReference)dictionaryRequestSchema.AdditionalProperties).Reference.Id);
            Assert.Equal("LocationContainer", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for nested LocationDto
            var locationContainerSchema = requestSchema;
            Assert.Equal("LocationDto", ((OpenApiSchemaReference)locationContainerSchema.Properties["location"]).Reference.Id);

            // Assert that $ref is used for nested AddressDto
            var locationSchema = locationContainerSchema.Properties["location"];
            Assert.Equal("AddressDto", ((OpenApiSchemaReference)locationSchema.Properties["address"]).Reference.Id);

            // Assert that $ref is used for related LocationDto
            var addressSchema = locationSchema.Properties["address"];
            Assert.Equal("LocationDto", ((OpenApiSchemaReference)addressSchema.Properties["relatedLocation"]).Reference.Id);

            // Assert that only expected schemas are generated at the top-level
            Assert.Equal(3, document.Components.Schemas.Count);
            Assert.Collection(document.Components.Schemas.Keys,
                key => Assert.Equal("AddressDto", key),
                key => Assert.Equal("LocationContainer", key),
                key => Assert.Equal("LocationDto", key));
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/60381
    [Fact]
    public async Task ResolvesListBasedReferencesCorrectly()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (ContainerType item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("ContainerType", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Get effective schema for ContainerType
            Assert.Equal(2, requestSchema.Properties.Count);

            // Check Seq1 and Seq2 properties
            var seq1Schema = requestSchema.Properties["seq1"];
            var seq2Schema = requestSchema.Properties["seq2"];

            // Assert both are array types
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, seq1Schema.Type);
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, seq2Schema.Type);

            // Assert items are arrays of strings
            Assert.Equal(JsonSchemaType.Array, seq1Schema.Items.Type);
            Assert.Equal(JsonSchemaType.Array, seq2Schema.Items.Type);

            // Since both Seq1 and Seq2 are the same type (List<List<string>>),
            // they should reference the same schema structure
            Assert.Equal(seq1Schema.Items.Type, seq2Schema.Items.Type);

            // Verify the inner arrays contain strings
            Assert.Equal(JsonSchemaType.String, seq1Schema.Items.Items.Type);
            Assert.Equal(JsonSchemaType.String, seq2Schema.Items.Items.Type);

            Assert.Equal(["ContainerType"], document.Components.Schemas.Keys);
        });
    }

    // Tests for: https://github.com/dotnet/aspnetcore/issues/60012
    [Fact]
    public async Task SupportsListOfClassInSelfReferentialSchema()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (Category item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("Category", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for nested Tags
            Assert.Equal("Tag", ((OpenApiSchemaReference)requestSchema.Properties["tags"].Items).Reference.Id);

            // Assert that $ref is used for nested Parent
            Assert.Equal("Category", ((OpenApiSchemaReference)requestSchema.Properties["parent"]).Reference.Id);

            // Assert that no duplicate schemas are emitted
            Assert.Collection(document.Components.Schemas,
                schema =>
                {
                    Assert.Equal("Category", schema.Key);
                },
                schema =>
                {
                    Assert.Equal("Tag", schema.Key);
                });
        });
    }

    [Fact]
    public async Task UsesSameReferenceForSameTypeInDifferentLocations()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/parent-object", (ParentObject item) => { });
        builder.MapPost("/list", (List<ParentObject> item) => { });
        builder.MapPost("/dictionary", (Dictionary<string, ParentObject> item) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/parent-object"].Operations[HttpMethod.Post];
            var requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Assert that $ref is used for nested Children
            Assert.Equal("ChildObject", ((OpenApiSchemaReference)requestSchema.Properties["children"].Items).Reference.Id);

            // Assert that $ref is used for nested Parent
            var childSchema = requestSchema.Properties["children"].Items;
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)childSchema.Properties["parent"]).Reference.Id);

            operation = document.Paths["/list"].Operations[HttpMethod.Post];
            requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for items in the list definition
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)requestSchema.Items).Reference.Id);
            var parentSchema = requestSchema.Items;
            Assert.Equal("ChildObject", ((OpenApiSchemaReference)parentSchema.Properties["children"].Items).Reference.Id);

            childSchema = parentSchema.Properties["children"].Items;
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)childSchema.Properties["parent"]).Reference.Id);

            operation = document.Paths["/dictionary"].Operations[HttpMethod.Post];
            requestSchema = operation.RequestBody.Content["application/json"].Schema;

            // Assert $ref used for items in the dictionary definition
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)requestSchema.AdditionalProperties).Reference.Id);
            parentSchema = requestSchema.AdditionalProperties;
            Assert.Equal("ChildObject", ((OpenApiSchemaReference)parentSchema.Properties["children"].Items).Reference.Id);

            childSchema = parentSchema.Properties["children"].Items;
            Assert.Equal("ParentObject", ((OpenApiSchemaReference)childSchema.Properties["parent"]).Reference.Id);

            // Assert that only the expected schemas are registered
            Assert.Equal(["ChildObject", "ParentObject"], document.Components.Schemas.Keys);
        });
    }

    private class Category
    {
        public required string Name { get; set; }

        public Category Parent { get; set; }

        public IEnumerable<Tag> Tags { get; set; } = [];
    }

    public class Tag
    {
        public required string Name { get; set; }
    }

    private class ContainerType
    {
        public List<List<string>> Seq1 { get; set; } = [];
        public List<List<string>> Seq2 { get; set; } = [];
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

    // Test for: https://github.com/dotnet/aspnetcore/issues/61194
    [Fact]
    public async Task ResolveGenericTypesInListProperties()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (Config config) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"]?.Operations?[HttpMethod.Post];
            var requestSchema = operation?.RequestBody?.Content?["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.NotNull(requestSchema);
            Assert.Equal("Config", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Get effective schema for Config
            Assert.Equal(2, requestSchema.Properties!.Count);

            // Check Items1 and Items2 properties
            var items1Schema = requestSchema.Properties!["items1"];
            var items2Schema = requestSchema.Properties!["items2"];

            // Assert both are array types
            Assert.Equal(JsonSchemaType.Array, items1Schema.Type);
            Assert.Equal(JsonSchemaType.Array, items2Schema.Type);

            // Assert items reference the same ConfigItem schema
            Assert.Equal("ConfigItem", ((OpenApiSchemaReference)items1Schema.Items!).Reference.Id);
            Assert.Equal("ConfigItem", ((OpenApiSchemaReference)items2Schema.Items!).Reference.Id);

            // Verify the ConfigItem schema has proper content, not empty
            var itemSchema = items1Schema.Items!;
            Assert.True(itemSchema.Properties?.Count > 0, "ConfigItem schema should not be empty");
            Assert.Contains("id", itemSchema.Properties?.Keys ?? []);
            Assert.Contains("lang", itemSchema.Properties?.Keys ?? []);
            Assert.Contains("words", itemSchema.Properties?.Keys ?? []);
            Assert.Contains("break", itemSchema.Properties?.Keys ?? []);
            Assert.Contains("willBeGood", itemSchema.Properties?.Keys ?? []);

            Assert.Equal(["Config", "ConfigItem"], document.Components!.Schemas!.Keys.OrderBy(x => x));
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/63054
    [Fact]
    public async Task ResolveReusedTypesAcrossDifferentHierarchies()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (ProjectResponse project) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths?["/"].Operations?[HttpMethod.Post];
            var requestSchema = operation?.RequestBody?.Content?["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.NotNull(requestSchema);
            Assert.Equal("ProjectResponse", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Check Address property
            var addressSchema = requestSchema.Properties!["address"];
            Assert.Equal("AddressResponse", ((OpenApiSchemaReference)addressSchema).Reference.Id);

            // Check Builder property
            var builderSchema = requestSchema.Properties!["builder"];
            Assert.Equal("BuilderResponse", ((OpenApiSchemaReference)builderSchema).Reference.Id);

            // Verify CityResponse is properly referenced in Address
            var cityInAddressSchema = addressSchema.Properties!["city"];
            Assert.Equal("CityResponse", ((OpenApiSchemaReference)cityInAddressSchema).Reference.Id);

            // Verify CityResponse is properly referenced in Builder
            var cityInBuilderSchema = builderSchema.Properties!["city"];
            Assert.Equal("CityResponse", ((OpenApiSchemaReference)cityInBuilderSchema).Reference.Id);

            // Verify the CityResponse schema has proper content, not empty
            var citySchema = cityInAddressSchema;
            Assert.True(citySchema.Properties?.Count > 0, "CityResponse schema should not be empty");
            Assert.Contains("name", citySchema.Properties?.Keys ?? []);

            Assert.Equal(["AddressResponse", "BuilderResponse", "CityResponse", "ProjectResponse"],
                        document.Components!.Schemas!.Keys.OrderBy(x => x));
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/63211
    [Fact]
    public async Task ResolveNullableReferenceTypes()
    {
        // Arrange
        var builder = CreateBuilder();

        builder.MapPost("/", (Subscription subscription) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths?["/"].Operations?[HttpMethod.Post];
            var requestSchema = operation?.RequestBody?.Content?["application/json"].Schema;

            // Assert $ref used for top-level
            Assert.NotNull(requestSchema);
            Assert.Equal("Subscription", ((OpenApiSchemaReference)requestSchema).Reference.Id);

            // Check primaryUser property (required RefProfile)
            var primaryUserSchema = requestSchema.Properties!["primaryUser"];
            Assert.Equal("RefProfile", ((OpenApiSchemaReference)primaryUserSchema).Reference.Id);

            // Check secondaryUser property (nullable RefProfile)
            var secondaryUserSchema = requestSchema.Properties!["secondaryUser"];
            Assert.NotNull(secondaryUserSchema.OneOf);
            Assert.Collection(secondaryUserSchema.OneOf,
                item => Assert.Equal(JsonSchemaType.Null, item.Type),
                item => Assert.Equal("RefProfile", ((OpenApiSchemaReference)item).Reference.Id));

            // Verify the RefProfile schema has a User property that references RefUser
            var userPropertySchema = primaryUserSchema.Properties!["user"];
            Assert.Equal("RefUser", ((OpenApiSchemaReference)userPropertySchema).Reference.Id);

            // Verify the RefUser schema has proper content, not empty
            var userSchemaContent = userPropertySchema;
            Assert.True(userSchemaContent.Properties?.Count > 0, "RefUser schema should not be empty");
            Assert.Contains("name", userSchemaContent.Properties?.Keys ?? []);
            Assert.Contains("email", userSchemaContent.Properties?.Keys ?? []);

            // Both properties should reference the same RefProfile schema
            var secondaryUserSchemaRef = secondaryUserSchema.OneOf.Last();
            Assert.Equal(((OpenApiSchemaReference)primaryUserSchema).Reference.Id,
                        ((OpenApiSchemaReference)secondaryUserSchemaRef).Reference.Id);

            Assert.Equal(["RefProfile", "RefUser", "Subscription"], document.Components!.Schemas!.Keys.OrderBy(x => x));
            Assert.All(document.Components.Schemas.Values, item => Assert.False(item.Type?.HasFlag(JsonSchemaType.Null)));
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/63503
    [Fact]
    public async Task HandlesCircularReferencesRegardlessOfPropertyOrder_SelfFirst()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelSelfFirst dto) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.NotNull(document.Components?.Schemas);
            var schema = document.Components.Schemas["DirectCircularModelSelfFirst"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.NotNull(schema.Properties);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("self", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelSelfFirst", reference.Reference.ReferenceV3);
                },
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotNull(referencedSchema.Properties);
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/63503
    [Fact]
    public async Task HandlesCircularReferencesRegardlessOfPropertyOrder_SelfLast()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelSelfLast dto) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.NotNull(document.Components?.Schemas);
            var schema = document.Components.Schemas["DirectCircularModelSelfLast"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.NotNull(schema.Properties);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                },
                property =>
                {
                    Assert.Equal("self", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelSelfLast", reference.Reference.ReferenceV3);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotNull(referencedSchema.Properties);
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/63503
    [Fact]
    public async Task HandlesCircularReferencesRegardlessOfPropertyOrder_MultipleSelf()
    {
        var builder = CreateBuilder();
        builder.MapPost("/", (DirectCircularModelMultiple dto) => { });

        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.NotNull(document.Components?.Schemas);
            var schema = document.Components.Schemas["DirectCircularModelMultiple"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.NotNull(schema.Properties);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("selfFirst", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelMultiple", reference.Reference.ReferenceV3);
                },
                property =>
                {
                    Assert.Equal("referenced", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                },
                property =>
                {
                    Assert.Equal("selfLast", property.Key);
                    var reference = Assert.IsType<OpenApiSchemaReference>(property.Value);
                    Assert.Equal("#/components/schemas/DirectCircularModelMultiple", reference.Reference.ReferenceV3);
                });

            // Verify that it does not result in an empty schema for a referenced schema
            var referencedSchema = document.Components.Schemas["ReferencedModel"];
            Assert.NotNull(referencedSchema.Properties);
            Assert.NotEmpty(referencedSchema.Properties);
            var idProperty = Assert.Single(referencedSchema.Properties);
            Assert.Equal("id", idProperty.Key);
            var idPropertySchema = Assert.IsType<OpenApiSchema>(idProperty.Value);
            Assert.Equal(JsonSchemaType.Integer, idPropertySchema.Type);
        });
    }

    // Test for: https://github.com/dotnet/aspnetcore/issues/64048
    public static object[][] CircularReferencesWithArraysHandlers =>
    [
        [(CircularReferenceWithArrayRootOrderArrayFirst dto) => { }],
        [(CircularReferenceWithArrayRootOrderArrayLast dto) => { }],
    ];

    [Theory]
    [MemberData(nameof(CircularReferencesWithArraysHandlers))]
    public async Task HandlesCircularReferencesWithArraysRegardlessOfPropertyOrder(Delegate requestHandler)
    {
        var builder = CreateBuilder();
        builder.MapPost("/", requestHandler);

        await VerifyOpenApiDocument(builder, (OpenApiDocument document) =>
        {
            Assert.NotNull(document.Components?.Schemas);
            var schema = document.Components.Schemas["CircularReferenceWithArrayModel"];
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.NotNull(schema.Properties);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("selfArray", property.Key);
                    var arraySchema = Assert.IsType<OpenApiSchema>(property.Value);
                    Assert.Equal(JsonSchemaType.Array, arraySchema.Type);
                    var itemReference = Assert.IsType<OpenApiSchemaReference>(arraySchema.Items);
                    Assert.Equal("#/components/schemas/CircularReferenceWithArrayModel", itemReference.Reference.ReferenceV3);
                });
        });
    }

    // Test models for issue 61194
    private class Config
    {
        public List<ConfigItem> Items1 { get; set; } = [];
        public List<ConfigItem> Items2 { get; set; } = [];
    }

    private class ConfigItem
    {
        public int? Id { get; set; }
        public string? Lang { get; set; }
        public Dictionary<string, object?>? Words { get; set; }
        public List<string>? Break { get; set; }
        public string? WillBeGood { get; set; }
    }

    // Test models for issue 63054
    private class ProjectResponse
    {
        public AddressResponse Address { get; init; } = new();
        public BuilderResponse Builder { get; init; } = new();
    }

    private class AddressResponse
    {
        public CityResponse City { get; init; } = new();
    }

    private class BuilderResponse
    {
        public CityResponse City { get; init; } = new();
    }

    private class CityResponse
    {
        public string Name { get; set; } = "";
    }

    // Test models for issue 63211
    public sealed class Subscription
    {
        public required string Id { get; set; }
        public required RefProfile PrimaryUser { get; set; }
        public RefProfile? SecondaryUser { get; set; }
    }

    public sealed class RefProfile
    {
        public required RefUser User { get; init; }
    }

    public sealed class RefUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    // Test models for issue 63503
    private class DirectCircularModelSelfFirst
    {
        public DirectCircularModelSelfFirst Self { get; set; } = null!;
        public ReferencedModel Referenced { get; set; } = null!;
    }

    private class DirectCircularModelSelfLast
    {
        public ReferencedModel Referenced { get; set; } = null!;
        public DirectCircularModelSelfLast Self { get; set; } = null!;
    }

    private class DirectCircularModelMultiple
    {
        public DirectCircularModelMultiple SelfFirst { get; set; } = null!;
        public ReferencedModel Referenced { get; set; } = null!;
        public DirectCircularModelMultiple SelfLast { get; set; } = null!;
    }

    private class ReferencedModel
    {
        public int Id { get; set; }
    }

    // Test models for issue 64048
    public class CircularReferenceWithArrayRootOrderArrayLast
    {
        public CircularReferenceWithArrayModel Item { get; set; } = null!;
        public ICollection<CircularReferenceWithArrayModel> ItemArray { get; set; } = [];
    }

    public class CircularReferenceWithArrayRootOrderArrayFirst
    {
        public ICollection<CircularReferenceWithArrayModel> ItemArray { get; set; } = [];
        public CircularReferenceWithArrayModel Item { get; set; } = null!;
    }

    public class CircularReferenceWithArrayModel
    {
        public ICollection<CircularReferenceWithArrayModel> SelfArray { get; set; } = [];
    }
}
#nullable restore
