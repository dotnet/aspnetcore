// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.References;

public class CustomSchemaTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task CustomSchemaTransformer_CanInsertSchemaIntoDocumentFromOperationTransformer()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapGet("/error", () => { });

        // Act
        var options = new OpenApiOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            if (context.Description.RelativePath == "error")
            {
                var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
                context.Document.AddComponent("Error", errorSchema);
                operation.Responses["500"] = new OpenApiResponse
                {
                    Description = "Error",
                    Content =
                    {
                        ["application/problem+json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchemaReference("Error", context.Document),
                        },
                    },
                };
            }
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            var schema = Assert.Single(document.Components.Schemas);
            Assert.Equal("Error", schema.Key);
            var targetSchema = Assert.IsType<OpenApiSchema>(schema.Value);
            Assert.Collection(targetSchema.Properties,
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
                });
        });
    }

    [Fact]
    public async Task GetOrCreateSchema_AddsSchemasForMultipleResponseTypes()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapGet("/api", () => TypedResults.Ok(new Todo(1, "Task", false, DateTime.Now)));

        // Act
        var options = new OpenApiOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            var todoSchema = await context.GetOrCreateSchemaAsync(typeof(Todo), cancellationToken: cancellationToken);
            context.Document.AddComponent("Todo2", todoSchema);

            var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
            context.Document.AddComponent("ProblemDetails", errorSchema);

            // Add both success and error responses
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Success",
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("Todo2", context.Document),
                    },
                },
            };

            operation.Responses["400"] = new OpenApiResponse
            {
                Description = "Bad Request",
                Content =
                {
                    ["application/problem+json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("ProblemDetails", context.Document),
                    },
                },
            };
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            Assert.Collection(document.Components.Schemas.Keys,
                key => Assert.Equal("ProblemDetails", key),
                key => Assert.Equal("Todo", key),
                key => Assert.Equal("Todo2", key));

            var todoSchema = document.Components.Schemas["Todo2"];
            Assert.Equal(4, todoSchema.Properties.Count);
            Assert.True(todoSchema.Properties.ContainsKey("id"));
            Assert.True(todoSchema.Properties.ContainsKey("title"));
            Assert.True(todoSchema.Properties.ContainsKey("completed"));
            Assert.True(todoSchema.Properties.ContainsKey("createdAt"));
        });
    }

    [Fact]
    public async Task GetOrCreateSchema_CanBeUsedInSchemaTransformer()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapPost("/shape", (Shape shape) => new Triangle { Hypotenuse = 25 });

        // Act
        var options = new OpenApiOptions();
        options.AddSchemaTransformer(async (schema, context, cancellationToken) =>
        {
            // Only transform the base Shape class schema
            if (context.JsonTypeInfo.Type == typeof(Shape))
            {
                // Create an example schema to reference in our documentation
                var exampleSchema = await context.GetOrCreateSchemaAsync(typeof(Triangle), cancellationToken: cancellationToken);
                context.Document.AddComponent("TriangleExample", exampleSchema);

                // Add a reference to the example in the shape schema
                schema.Extensions["x-example-component"] = new OpenApiAny("#/components/schemas/TriangleExample");
                schema.Description = "A shape with an example reference";
            }
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            // Verify we have our TriangleExample component
            Assert.True(document.Components.Schemas.ContainsKey("TriangleExample"));

            // Verify the base Shape schema has our extension
            var shapeSchema = document.Components.Schemas["Shape"];

            Assert.NotNull(shapeSchema);
            Assert.Equal("A shape with an example reference", shapeSchema.Description);
            Assert.True(shapeSchema.Extensions.ContainsKey("x-example-component"));
        });
    }

    [Fact]
    public async Task GetOrCreateSchema_CreatesDifferentSchemaForSameTypeWithDifferentParameterDescription()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapPost("/items", (int id, [FromQuery] int limit) => { });

        // Act
        var options = new OpenApiOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            // Get the parameter descriptions associated with the type
            var idParam = context.Description.ParameterDescriptions.FirstOrDefault(p => p.Name == "id");
            var limitParam = context.Description.ParameterDescriptions.FirstOrDefault(p => p.Name == "limit");

            // Get schemas for same type but different parameter descriptions
            var idSchema = await context.GetOrCreateSchemaAsync(typeof(int), idParam, cancellationToken);
            var limitSchema = await context.GetOrCreateSchemaAsync(typeof(int), limitParam, cancellationToken);

            // Add schemas to document
            context.Document.AddComponent("IdParameter", idSchema);
            context.Document.AddComponent("LimitParameter", limitSchema);

            // Use schemas in custom parameter
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "custom-id",
                In = ParameterLocation.Path,
                Schema = new OpenApiSchemaReference("IdParameter", context.Document)
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "custom-limit",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchemaReference("LimitParameter", context.Document)
            });
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            Assert.Equal(2, document.Components.Schemas.Count);
            Assert.Contains("IdParameter", document.Components.Schemas.Keys);
            Assert.Contains("LimitParameter", document.Components.Schemas.Keys);

            // Both schemas should have the same base type properties
            var idSchema = document.Components.Schemas["IdParameter"];
            var limitSchema = document.Components.Schemas["LimitParameter"];

            Assert.Equal(JsonSchemaType.Integer, idSchema.Type);
            Assert.Equal(JsonSchemaType.Integer, limitSchema.Type);

            // Operation should now have 4 parameters (2 original + 2 custom)
            var operation = document.Paths["/items"].Operations[OperationType.Post];
            Assert.Equal(4, operation.Parameters.Count);
        });
    }

    [Fact]
    public async Task GetOrCreateSchema_WorksWithNestedTypes()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapGet("/api", () => new { });

        // Act
        var options = new OpenApiOptions();
        options.AddDocumentTransformer(async (document, context, cancellationToken) =>
        {
            // Generate schema for a complex nested type
            var nestedTypeSchema = await context.GetOrCreateSchemaAsync(typeof(NestedContainer), cancellationToken: cancellationToken);
            document.AddComponent("NestedContainer", nestedTypeSchema);

            // Add a new path that uses this schema
            var path = new OpenApiPathItem();
            var operation = new OpenApiOperation
            {
                OperationId = "GetNestedContainer",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Success",
                        Content =
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchemaReference("NestedContainer", document)
                            }
                        }
                    }
                }
            };

            path.Operations[OperationType.Get] = operation;
            document.Paths["/nested"] = path;
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            // Verify the schema was added
            Assert.True(document.Components.Schemas.ContainsKey("NestedContainer"));

            // Verify the path was added
            Assert.True(document.Paths.ContainsKey("/nested"));

            // Verify the schema structure
            var containerSchema = document.Components.Schemas["NestedContainer"];
            Assert.True(containerSchema.Properties.ContainsKey("items"));

            // Verify array type for items
            var itemsSchema = containerSchema.Properties["items"];
            Assert.Equal(JsonSchemaType.Array | JsonSchemaType.Null, itemsSchema.Type);

            // Component schemas are not generated for nested types
            Assert.False(document.Components.Schemas.ContainsKey("NestedItem"));
            Assert.True(itemsSchema.Items is OpenApiSchema);
        });
    }

    [Fact]
    public async Task GetOrCreateSchemaAsync_AppliesOtherSchemaTransformers()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapGet("/product", () => new { });

        // Define a transformation flag that we'll check later
        var transformerApplied = false;
        var nestedTransformerApplied = false;

        // Act
        var options = new OpenApiOptions();

        // Add a schema transformer that will mark all Product schemas as required
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Product))
            {
                schema.Required.Add("name");
                schema.Required.Add("price");
                transformerApplied = true;
            }

            if (context.JsonTypeInfo.Type == typeof(Category))
            {
                schema.Description = "Transformed category description";
                nestedTransformerApplied = true;
            }

            return Task.CompletedTask;
        });

        // Add an operation transformer that uses GetOrCreateSchemaAsync
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            // Generate a schema for Product
            var productSchema = await context.GetOrCreateSchemaAsync(typeof(Product), cancellationToken: cancellationToken);

            // Add it to the document
            context.Document.AddComponent("ProductSchema", productSchema);

            // Use it in the response
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "A product",
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("ProductSchema", context.Document)
                    }
                }
            };
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            // Verify schema was created
            Assert.True(document.Components.Schemas.ContainsKey("ProductSchema"));

            // Get the schema
            var schema = document.Components.Schemas["ProductSchema"];

            // Verify schema properties
            Assert.True(schema.Properties.ContainsKey("name"));
            Assert.True(schema.Properties.ContainsKey("price"));
            Assert.True(schema.Properties.ContainsKey("category"));

            // Verify transformer was applied - it should have added required properties
            Assert.True(transformerApplied);
            Assert.Contains("name", schema.Required);
            Assert.Contains("price", schema.Required);

            // Verify transformer was also applied to nested schema
            var categoryProperty = schema.Properties["category"];
            Assert.True(nestedTransformerApplied);

            Assert.Equal("Transformed category description", categoryProperty.Description);
        });
    }

    [Fact]
    public async Task GetOrCreateSchemaAsync_HandlesConcurrentRequests()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.MapGet("/concurrent", () => new { });

        // Act
        var options = new OpenApiOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            // Generate schema concurrently for the same type
            var tasks = new Task<OpenApiSchema>[5];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = context.GetOrCreateSchemaAsync(typeof(ComplexType), cancellationToken: cancellationToken);
            }

            // Wait for all tasks to complete
            var schemas = await Task.WhenAll(tasks);

            // All schemas should be the same instance when added to components
            for (var i = 0; i < schemas.Length; i++)
            {
                context.Document.AddComponent($"Schema{i}", schemas[i]);
            }

            // Use one of them in the response
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Concurrent schema generation test",
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("Schema0", context.Document)
                    }
                }
            };
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            // All schemas should be generated
            for (var i = 0; i < 5; i++)
            {
                Assert.True(document.Components.Schemas.ContainsKey($"Schema{i}"));
                // They should all have the same structure
                var schema = document.Components.Schemas[$"Schema{i}"];

                Assert.True(schema.Properties.ContainsKey("id"));
                Assert.True(schema.Properties.ContainsKey("name"));
                Assert.True(schema.Properties.ContainsKey("createdAt"));
                Assert.True(schema.Properties.ContainsKey("tags"));
                Assert.True(schema.Properties.ContainsKey("metadata"));
            }
        });
    }

    [Fact]
    public async Task GetOrCreateSchemaAsync_RespectsJsonSerializerOptions()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        var builder = CreateBuilder();
        builder.MapGet("/customjson", () => new { });

        // Act
        var options = new OpenApiOptions();
        options.AddOperationTransformer(async (operation, context, cancellationToken) =>
        {
            // Generate schema that should respect JSON naming policy
            var userSchema = await context.GetOrCreateSchemaAsync(typeof(UserWithJsonOptions), cancellationToken: cancellationToken);
            context.Document.AddComponent("User", userSchema);

            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "User with custom JSON options",
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("User", context.Document)
                    }
                }
            };
        });

        // Assert
        await VerifyOpenApiDocument(builder, options, (document) =>
        {
            // Verify schema was created
            Assert.True(document.Components.Schemas.ContainsKey("User"));

            // Get the schema
            var schema = document.Components.Schemas["User"];

            // Property names should be camelCase due to the naming policy
            Assert.True(schema.Properties.ContainsKey("firstName"));
            Assert.True(schema.Properties.ContainsKey("lastName"));
            Assert.True(schema.Properties.ContainsKey("dateOfBirth"));

            // The ignored property should not be in the schema
            Assert.False(schema.Properties.ContainsKey("temporaryData"));
        });
    }

    // For the nested types test
    internal class NestedContainer
    {
        public List<NestedItem> Items { get; set; } = [];
        public string Name { get; set; } = "Container";
    }

    internal class NestedItem
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    // Supporting classes for the test
    internal class Product
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Category Category { get; set; } = new();
    }

    internal class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    internal class ComplexType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, object> Metadata { get; set; } = [];
    }

    internal class UserWithJsonOptions
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }

        [JsonIgnore]
        public string TemporaryData { get; set; } = string.Empty;
    }

}

