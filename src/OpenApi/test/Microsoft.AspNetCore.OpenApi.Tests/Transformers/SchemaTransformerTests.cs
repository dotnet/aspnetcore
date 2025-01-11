// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public class SchemaTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task SchemaTransformer_CanAccessTypeAndParameterDescriptionForParameter()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });

        var options = new OpenApiOptions();
        var firstInvocationOnSecondTransformer = true;
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            ValidateContext(context);
            return Task.CompletedTask;
        })
        .AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            // Coverage for https://github.com/dotnet/aspnetcore/issues/56899
            if (firstInvocationOnSecondTransformer)
            {
                Assert.Equal(typeof(Todo), context.JsonTypeInfo.Type);
                firstInvocationOnSecondTransformer = false;
            }
            // Rest of the state is still consistent
            ValidateContext(context);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });

        static void ValidateContext(OpenApiSchemaTransformerContext context)
        {
            if (context.JsonPropertyInfo == null)
            {
                Assert.Equal(typeof(Todo), context.JsonTypeInfo.Type);
                Assert.Equal("todo", context.ParameterDescription.Name);
            }
            if (context.JsonPropertyInfo?.Name == "id")
            {
                Assert.Equal(typeof(int), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "name")
            {
                Assert.Equal(typeof(string), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "isComplete")
            {
                Assert.Equal(typeof(bool), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "dueDate")
            {
                Assert.Equal(typeof(DateTime), context.JsonTypeInfo.Type);
            }
        }
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessTypeForResponse()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonPropertyInfo == null)
            {
                Assert.Equal(typeof(Todo), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "id")
            {
                Assert.Equal(typeof(int), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "name")
            {
                Assert.Equal(typeof(string), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "isComplete")
            {
                Assert.Equal(typeof(bool), context.JsonTypeInfo.Type);
            }
            if (context.JsonPropertyInfo?.Name == "dueDate")
            {
                Assert.Equal(typeof(DateTime), context.JsonTypeInfo.Type);
            }
            Assert.Null(context.ParameterDescription);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessApplicationServicesAndDocumentName()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetKeyedService<OpenApiDocumentService>(context.DocumentName);
            Assert.NotNull(service);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_RespectsCancellationToken()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(cts.Token, cancellationToken);
            Assert.True(cancellationToken.IsCancellationRequested);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { }, cts.Token);
    }

    [Fact]
    public async Task SchemaTransformer_RunsInRegisteredOrder()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            schema.Extensions["x-my-extension"] = new OpenApiAny("1");
            schema.Format = "1";
            return Task.CompletedTask;
        });
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal("1", ((OpenApiAny)schema.Extensions["x-my-extension"]).Node.GetValue<string>());
            schema.Extensions["x-my-extension"] = new OpenApiAny("2");
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations.Values.Single();
            var schema = operation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("2", ((OpenApiAny)schema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_OnTypeModifiesBothRequestAndResponse()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("1");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)responseSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [ConditionalFact(Skip = "SchemaTransformer_WithDescriptionOnlyModifiesParameter")]
    public async Task SchemaTransformer_WithDescriptionOnlyModifiesParameter()
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
            Assert.Equal("todo", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.False(responseSchema.Extensions.TryGetValue("x-my-extension", out var _));
        });
    }

    [Fact]
    public async Task SchemaTransformer_SupportsActivatedTransformers()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer<ActivatedTransformer>();

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)responseSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_SupportsInstanceTransformers()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer(new ActivatedTransformer());

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("1", ((OpenApiAny)responseSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_SupportsActivatedTransformerWithSingletonDependency()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer<ActivatedTransformerWithDependency>();

        // Assert that singleton dependency is only instantiated once
        // regardless of the number of requests, operations or schemas.
        string value = null;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            value = ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>();
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal(value, ((OpenApiAny)responseSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal(value, ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal(value, ((OpenApiAny)responseSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_SupportsActivatedTransformerWithTransientDependency()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer<ActivatedTransformerWithDependency>();

        Dependency.InstantiationCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.True(requestSchema.Extensions.ContainsKey("x-my-extension"));
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.True(responseSchema.Extensions.ContainsKey("x-my-extension"));
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.True(requestSchema.Extensions.ContainsKey("x-my-extension"));
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.True(responseSchema.Extensions.ContainsKey("x-my-extension"));
        });
        // Assert that the transient dependency has a "scoped" lifetime within
        // the context of the transformer and is called twice, once for each request.
        Assert.Equal(2, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task SchemaTransformer_SupportsDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer<DisposableTransformer>();

        DisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("Schema Description", requestSchema.Description);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("Schema Description", responseSchema.Description);
        });
        // Assert that the transformer is disposed once for the entire document.
        Assert.Equal(1, DisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task SchemaTransformer_SupportsAsyncDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer<AsyncDisposableTransformer>();

        AsyncDisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("Schema Description", requestSchema.Description);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("Schema Description", responseSchema.Description);
        });
        // Assert that the transformer is disposed once for the entire document.
        Assert.Equal(1, AsyncDisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyAllTypesInADocument()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", (int id) => { });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(int))
            {
                schema.Format = "modified-number-format";
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that parameter schema has been update
            var path = Assert.Single(document.Paths.Values);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Parameters[0].Schema;
            Assert.Equal("modified-number-format", responseSchema.Format);

            // Assert that property in request body schema has been updated
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("modified-number-format", requestSchema.Properties["id"].Format);
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyItemTypesInADocument()
    {
        var builder = CreateBuilder();

        builder.MapGet("/list", () => new List<int> { 1, 2, 3, 4 });
        builder.MapGet("/single", () => 1);
        builder.MapGet("/dictionary", () => new Dictionary<string, int> { { "key", 1 } });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(int))
            {
                schema.Format = "modified-number-format";
            }
            schema = new OpenApiSchema { Type = JsonSchemaType.Array, Items = schema };
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the schema represent list elements has been modified
            var path = document.Paths["/list"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("modified-number-format", responseSchema.Items.Format);

            // Assert that top-level schema associated with the standalone integer has been updated
            path = document.Paths["/single"];
            getOperation = path.Operations[OperationType.Get];
            responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("modified-number-format", responseSchema.Format);

            // Assert that the schema represent dictionary values has been modified
            path = document.Paths["/dictionary"];
            getOperation = path.Operations[OperationType.Get];
            responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.Equal("modified-number-format", responseSchema.AdditionalProperties.Format);
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyPolymorphicChildSchemas()
    {
        var builder = CreateBuilder();

        builder.MapPost("/shape", (Shape todo) => { });
        builder.MapPost("/triangle", (Triangle todo) => { });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Triangle))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-triangle");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the polymorphic sub-type `Triangle` has been updated
            var path = document.Paths["/shape"];
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            var triangleSubschema = Assert.Single(requestSchema.AnyOf.Where(s => s.Reference.Id == "ShapeTriangle"));
            Assert.True(triangleSubschema.Extensions.TryGetValue("x-my-extension", out var _));

            // Assert that the standalone `Triangle` type has been updated
            path = document.Paths["/triangle"];
            postOperation = path.Operations[OperationType.Post];
            requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("this-is-a-triangle", ((OpenApiAny)requestSchema.Extensions["x-my-extension"]).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyPropertiesInAnItemsType()
    {
        var builder = CreateBuilder();

        builder.MapGet("/list-of-todo", () => new List<Todo> { new Todo(1, "Item1", false, DateTime.Now) });
        builder.MapGet("/list-of-int", () => new List<int> { 1, 2, 3, 4 });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(int))
            {
                schema.Format = "modified-number-format";
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the `id` property in the `Todo` within the array has been updated
            var path = document.Paths["/list-of-todo"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            var itemSchema = responseSchema.Items;
            Assert.Equal("modified-number-format", itemSchema.Properties["id"].Format);

            // Assert that the integer type within the list has been updated
            var otherPath = document.Paths["/list-of-int"];
            var otherGetOperation = otherPath.Operations[OperationType.Get];
            var otherResponseSchema = otherGetOperation.Responses["200"].Content["application/json"].Schema;
            var otherItemSchema = otherResponseSchema.Items;
            Assert.Equal("modified-number-format", otherItemSchema.Format);
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyListOfPolymorphicTypes()
    {
        var builder = CreateBuilder();

        builder.MapGet("/list", () => new List<Shape> { new Triangle { Hypotenuse = 12, Color = "blue", Sides = 3 }, new Square { Area = 24, Color = "red ", Sides = 4 } });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Triangle))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-triangle");
            }
            if (context.JsonTypeInfo.Type == typeof(Square))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-square");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the `Triangle` type within the list has been updated
            var path = document.Paths["/list"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            var itemSchema = responseSchema.Items;
            var triangleSubschema = Assert.Single(itemSchema.AnyOf.Where(s => s.Reference.Id == "ShapeTriangle"));
            // Assert that the x-my-extension type is set to this-is-a-triangle
            Assert.True(triangleSubschema.Extensions.TryGetValue("x-my-extension", out var triangleExtension));
            Assert.Equal("this-is-a-triangle", ((OpenApiAny)triangleExtension).Node.GetValue<string>());

            // Assert that the `Square` type within the polymorphic type list has been updated
            var squareSubschema = Assert.Single(itemSchema.AnyOf.Where(s => s.Reference.Id == "ShapeSquare"));
            // Assert that the x-my-extension type is set to this-is-a-square
            Assert.True(squareSubschema.Extensions.TryGetValue("x-my-extension", out var squareExtension));
            Assert.Equal("this-is-a-square", ((OpenApiAny)squareExtension).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyPolymorphicTypesInProperties()
    {
        var builder = CreateBuilder();

        builder.MapGet("/list", () => new PolymorphicContainer());

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Triangle))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-triangle");
            }
            if (context.JsonTypeInfo.Type == typeof(Square))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-square");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the `Triangle` type within the list has been updated
            var path = document.Paths["/list"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            var someShapeSchema = responseSchema.Properties["someShape"];
            var triangleSubschema = Assert.Single(someShapeSchema.AnyOf.Where(s => s.Reference.Id == "ShapeTriangle"));
            // Assert that the x-my-extension type is set to this-is-a-triangle
            Assert.True(triangleSubschema.Extensions.TryGetValue("x-my-extension", out var triangleExtension));
            Assert.Equal("this-is-a-triangle", ((OpenApiAny)triangleExtension).Node.GetValue<string>());

            // Assert that the `Square` type within the polymorphic type list has been updated
            var squareSubschema = Assert.Single(someShapeSchema.AnyOf.Where(s => s.Reference.Id == "ShapeSquare"));
            // Assert that the x-my-extension type is set to this-is-a-square
            Assert.True(squareSubschema.Extensions.TryGetValue("x-my-extension", out var squareExtension));
            Assert.Equal("this-is-a-square", ((OpenApiAny)squareExtension).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformer_CanModifyDeeplyNestedPolymorphicTypesInProperties()
    {
        var builder = CreateBuilder();

        builder.MapGet("/list", () => new List<PolymorphicContainer>());

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Triangle))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-triangle");
            }
            if (context.JsonTypeInfo.Type == typeof(Square))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("this-is-a-square");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            // Assert that the `Triangle` type within the list has been updated
            var path = document.Paths["/list"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            var someShapeSchema = responseSchema.Items.Properties["someShape"];
            var triangleSubschema = Assert.Single(someShapeSchema.AnyOf.Where(s => s.Reference.Id == "ShapeTriangle"));
            // Assert that the x-my-extension type is set to this-is-a-triangle
            Assert.True(triangleSubschema.Extensions.TryGetValue("x-my-extension", out var triangleExtension));
            Assert.Equal("this-is-a-triangle", ((OpenApiAny)triangleExtension).Node.GetValue<string>());

            // Assert that the `Square` type within the polymorphic type list has been updated
            var squareSubschema = Assert.Single(someShapeSchema.AnyOf.Where(s => s.Reference.Id == "ShapeSquare"));
            // Assert that the x-my-extension type is set to this-is-a-square
            Assert.True(squareSubschema.Extensions.TryGetValue("x-my-extension", out var squareExtension));
            Assert.Equal("this-is-a-square", ((OpenApiAny)squareExtension).Node.GetValue<string>());
        });
    }

    [Fact]
    public async Task SchemaTransformers_CanModifyMultipleFormParameters()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", ([FromForm] Todo todo, [FromForm] Error error) => { });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(int))
            {
                schema.Format = "modified-number-format";
            }
            return Task.CompletedTask;
        });

        // We use `allOf` for multiple form parameters to ensure that they should be aggregated
        // appropriately in the request body schema. Although we don't handle `AllOf` when we apply
        // schema transformers, these modifications still work because the wrapping of these schemas into
        // allOf definitions happens after all transformers have been applied.
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = document.Paths["/todo"];
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
            Assert.Equal(2, requestSchema.AllOf.Count);
            var todoSchema = requestSchema.AllOf[0];
            var errorSchema = requestSchema.AllOf[1];
            Assert.Equal("modified-number-format", todoSchema.Properties["id"].Format);
            Assert.Equal("modified-number-format", errorSchema.Properties["code"].Format);
        });
    }

    [Fact]
    public async Task SchemaTransformers_CanImplementNotSchemaIndependently()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.JsonTypeInfo.Type == typeof(Todo))
            {
                schema.Not = new OpenApiSchema { Type = JsonSchemaType.String };
            }
            if (context.JsonTypeInfo.Type == typeof(Triangle))
            {
                schema.Not = new OpenApiSchema { Type = JsonSchemaType.String };
            }
            return Task.CompletedTask;
        });
        UseNotSchemaTransformer(options, (schema, context, cancellationToken) =>
        {
            schema.Extensions["modified-by-not-schema-transformer"] = new OpenApiAny(true);
            return Task.CompletedTask;
        });

        // Assert that not schemas have been modified for both `Todo` and `Triangle` types.
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = document.Paths["/todo"];
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.True(((OpenApiAny)responseSchema.Not.Extensions["modified-by-not-schema-transformer"]).Node.GetValue<bool>());

            var shapePath = document.Paths["/shape"];
            var shapeOperation = shapePath.Operations[OperationType.Post];
            var shapeRequestSchema = shapeOperation.RequestBody.Content["application/json"].Schema;
            var triangleSchema = Assert.Single(shapeRequestSchema.AnyOf.Where(s => s.Reference.Id == "ShapeTriangle"));
            Assert.True(((OpenApiAny)triangleSchema.Not.Extensions["modified-by-not-schema-transformer"]).Node.GetValue<bool>());
        });

        static void UseNotSchemaTransformer(OpenApiOptions options, Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task> func)
        {
            options.AddSchemaTransformer(async (schema, context, cancellationToken) =>
            {
                if (schema.Not != null)
                {
                    await func(schema.Not, context, cancellationToken);
                }
                return;
            });
        }
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessSingletonServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the singleton dependency is instantiated only once
        // for the entire lifetime of the application.
        Assert.Equal(1, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessScopedServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the scoped dependency is instantiated twice. Once for
        // each request to the document.
        Assert.Equal(2, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessTransientServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        // Assert that the transient dependency is invoked for each schema
        // in the document. In this case, we have five total schemas in the document.
        // One for the top-level `Todo` type and four for the properties of the `Todo` type.
        // Since we call GetRequiredService twice in the transformer, the total number of
        // instantiations should be 10.
        Assert.Equal(10, Dependency.InstantiationCount);
    }

    private class PolymorphicContainer
    {
        public string Name { get; }
        public Shape SomeShape { get; }
    }

    private class ActivatedTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonTypeInfo.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("1");
            }
            return Task.CompletedTask;
        }
    }

    private class DisposableTransformer : IOpenApiSchemaTransformer, IDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public void Dispose()
        {
            Disposed = true;
            DisposeCount += 1;
        }

        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            schema.Description = "Schema Description";
            return Task.CompletedTask;
        }
    }

    private class AsyncDisposableTransformer : IOpenApiSchemaTransformer, IAsyncDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            DisposeCount += 1;
            return ValueTask.CompletedTask;
        }

        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            schema.Description = "Schema Description";
            return Task.CompletedTask;
        }
    }

    private class ActivatedTransformerWithDependency(Dependency dependency) : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            dependency.TestMethod();
            schema.Extensions["x-my-extension"] = new OpenApiAny(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }
    }

    private class Dependency
    {
        public Dependency()
        {
            InstantiationCount += 1;
        }

        internal void TestMethod() { }

        internal static int InstantiationCount = 0;
    }
}
