// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
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
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(typeof(Todo), context.Type);
            Assert.Equal("todo", context.ParameterDescription.Name);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessTypeForResponse()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(typeof(Todo), context.Type);
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
            schema.Extensions["x-my-extension"] = new OpenApiString("1");
            return Task.CompletedTask;
        });
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal("1", ((OpenApiString)schema.Extensions["x-my-extension"]).Value);
            schema.Extensions["x-my-extension"] = new OpenApiString("2");
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations.Values.Single();
            var schema = operation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("2", ((OpenApiString)schema.Extensions["x-my-extension"]).Value);
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
            if (context.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiString("1");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
        });
    }

    [Fact]
    public async Task SchemaTransformer_WithDescriptionOnlyModifiesParameter()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.Type == typeof(Todo) && context.ParameterDescription is not null)
            {
                schema.Extensions["x-my-extension"] = new OpenApiString(context.ParameterDescription.Name);
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("todo", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
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
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
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
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
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
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            value = ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value;
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal(value, ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal(value, ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal(value, ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
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

        // Assert that transient dependency is instantiated once for each
        // request to the OpenAPI document for each created schema.
        var countBefore = Dependency.InstantiationCount;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.True(requestSchema.Extensions.ContainsKey("x-my-extension"));
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.True(responseSchema.Extensions.ContainsKey("x-my-extension"));
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.True(requestSchema.Extensions.ContainsKey("x-my-extension"));
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.True(responseSchema.Extensions.ContainsKey("x-my-extension"));
        });
        var countAfter = Dependency.InstantiationCount;
        Assert.Equal(countBefore + 4, countAfter);
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
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("Schema Description", requestSchema.Description);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("Schema Description", responseSchema.Description);
        });
        Assert.Equal(2, DisposableTransformer.DisposeCount);
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
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("Schema Description", requestSchema.Description);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("Schema Description", responseSchema.Description);
        });
        Assert.Equal(2, AsyncDisposableTransformer.DisposeCount);
    }

    private class ActivatedTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiString("1");
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
            schema.Extensions["x-my-extension"] = new OpenApiString(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture));
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
