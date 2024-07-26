// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

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

            var effectiveSchema = schema.GetEffective(document);
            Assert.Equal("object", effectiveSchema.Type);
            Assert.Equal(1, effectiveSchema.Properties.Count);
            var effectivePropertySchema = effectiveSchema.Properties["value"].GetEffective(document);
            Assert.Equal("string", effectivePropertySchema.Type);
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

            var effectiveSchema = requestBodySchema.GetEffective(document);
            Assert.Equal("object", effectiveSchema.Type);
            Assert.Equal(4, effectiveSchema.Properties.Count);
            var effectiveIdSchema = effectiveSchema.Properties["id"].GetEffective(document);
            Assert.Equal("integer", effectiveIdSchema.Type);
            var effectiveTitleSchema = effectiveSchema.Properties["title"].GetEffective(document);
            Assert.Equal("string", effectiveTitleSchema.Type);
            var effectiveCompletedSchema = effectiveSchema.Properties["completed"].GetEffective(document);
            Assert.Equal("boolean", effectiveCompletedSchema.Type);
            var effectiveCreatedAtSchema = effectiveSchema.Properties["createdAt"].GetEffective(document);
            Assert.Equal("string", effectiveCreatedAtSchema.Type);
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
            var requestBodySchema = requestBody.Schema.GetEffective(document);

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema.GetEffective(document);

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
            Assert.Equal("array", requestBodySchema.Type);
            Assert.Equal("object", requestBodySchema2.Type);
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
            Assert.Equal("object", requestBodySchema.AllOf[0].Type);
            Assert.Equal("string", requestBodySchema.AllOf[0].Properties["resume"].Type);
            Assert.Equal("binary", requestBodySchema.AllOf[0].Properties["resume"].Format);

            // string parameter is not resolved to a top-level reference.
            Assert.Equal("object", requestBodySchema2.AllOf[0].Type);
            Assert.Null(requestBodySchema.AllOf[1].GetEffective(document).Properties["title"].Reference);
            Assert.Null(requestBodySchema2.AllOf[1].GetEffective(document).Properties["title"].Reference);
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
            Assert.Equal("array", requestBodySchema.Type);
            // With an `items` sub-schema should consist of a $ref to Todo
            Assert.Equal("Todo", requestBodySchema.Items.Reference.Id);
            Assert.Equal(requestBodySchema.Items.Reference.Id, requestBodySchema2.Items.Reference.Id);
            Assert.Equal(4, requestBodySchema.Items.GetEffective(document).Properties.Count);
        });
    }

    [Fact]
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
                schema.Extensions["x-my-extension"] = new OpenApiString(context.ParameterDescription.Name);
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
            Assert.Equal("todo", ((OpenApiString)requestSchema.GetEffective(document).Extensions["x-my-extension"]).Value);
            Assert.False(responseSchema.GetEffective(document).Extensions.TryGetValue("x-my-extension", out var _));
        });
    }
}
