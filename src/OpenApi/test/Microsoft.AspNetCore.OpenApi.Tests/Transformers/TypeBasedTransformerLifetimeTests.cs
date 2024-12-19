// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public class TypeBasedTransformerLifetimeTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task ActivatedSchemaTransformerIsInitializedOncePerDocument()
    {
        var builder = CreateBuilder();
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddSchemaTransformer<ActivatedSchemaTransformer>();

        ActivatedSchemaTransformer.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is only instantiated once per document
        // even though there are multiple schemas in the document.
        Assert.Equal(1, ActivatedSchemaTransformer.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedSchemaTransformerWithSingletonDependencyIsInitializedForLifetime()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddSchemaTransformer<ActivatedSchemaTransformerWithDependency>();

        ActivatedSchemaTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedSchemaTransformerWithDependency.InstantiationCount);
        // Assert that the singleton dependency utilized initialized by the transformer is instantiated once.
        Assert.Equal(1, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedSchemaTransformerWithScopedDependencyIsInitializedPerScope()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddSchemaTransformer<ActivatedSchemaTransformerWithDependency>();

        ActivatedSchemaTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedSchemaTransformerWithDependency.InstantiationCount);
        // Assert that the scoped dependency is instantiated once per request.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedSchemaTransformerWithTransientDependencyIsInitializedPerRequest()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddSchemaTransformer<ActivatedSchemaTransformerWithDependency>();

        ActivatedSchemaTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedSchemaTransformerWithDependency.InstantiationCount);
        // Assert that the transient dependency is instantiated once per request.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedOperationTransformerIsInitializedOncePerDocument()
    {
        var builder = CreateBuilder();
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddOperationTransformer<ActivatedOperationTransformer>();

        ActivatedOperationTransformer.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is only instantiated once per document
        // even though there are 3 operations in the document.
        Assert.Equal(1, ActivatedOperationTransformer.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedOperationTransformerWithSingletonDependencyIsInitializedForLifetime()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });

        options.AddOperationTransformer<ActivatedOperationTransformerWithDependency>();

        ActivatedOperationTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedOperationTransformerWithDependency.InstantiationCount);
        // Assert that the singleton dependency utilized initialized by the transformer is instantiated once.
        Assert.Equal(1, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedOperationTransformerWithScopedDependencyIsInitializedPerScope()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddOperationTransformer<ActivatedOperationTransformerWithDependency>();

        ActivatedOperationTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedOperationTransformerWithDependency.InstantiationCount);
        // Assert that the singleton dependency utilized initialized by the transformer is instantiated once.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

        [Fact]
    public async Task ActivatedOperationTransformerWithTransientDependencyIsInitializedPerRequest()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddOperationTransformer<ActivatedOperationTransformerWithDependency>();

        ActivatedOperationTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedOperationTransformerWithDependency.InstantiationCount);
        // Assert that the transient dependency is instantiated once per request.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedDocumentTransformerIsInitializedOncePerDocument()
    {
        var builder = CreateBuilder();
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddDocumentTransformer<ActivatedDocumentTransformer>();

        ActivatedDocumentTransformer.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is only instantiated once per call
        // to generate the document.
        Assert.Equal(1, ActivatedDocumentTransformer.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedDocumentTransformerIsInitializedPerDocumentRequest()
    {
        var builder = CreateBuilder();
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddDocumentTransformer<ActivatedDocumentTransformer>();

        ActivatedDocumentTransformer.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is only instantiated twice, once per call
        // to generate the document.
        Assert.Equal(2, ActivatedDocumentTransformer.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedDocumentTransformerWithSingletonDependencyIsInitializedForLifetime()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddDocumentTransformer<ActivatedDocumentTransformerWithDependency>();

        ActivatedDocumentTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedDocumentTransformerWithDependency.InstantiationCount);
        // Assert that the singleton dependency utilized initialized by the transformer is instantiated once.
        Assert.Equal(1, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedDocumentTransformerWithScopedDependencyIsInitializedPerScope()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddDocumentTransformer<ActivatedDocumentTransformerWithDependency>();

        ActivatedDocumentTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedDocumentTransformerWithDependency.InstantiationCount);
        // Assert that the singleton dependency utilized initialized by the transformer is instantiated once.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task ActivatedDocumentTransformerWithTransientDependencyIsInitializedPerRequest()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);
        var options = new OpenApiOptions();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapPost("/shape", (Shape shape) => { });
        builder.MapPost("/triangle", (Triangle triangle) => { });

        options.AddDocumentTransformer<ActivatedDocumentTransformerWithDependency>();

        ActivatedDocumentTransformerWithDependency.InstantiationCount = 0;
        Dependency.InstantiationCount = 0;

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the transformer is instantiated three times, once for each request to the document.
        Assert.Equal(3, ActivatedDocumentTransformerWithDependency.InstantiationCount);
        // Assert that the transient dependency is instantiated once per request.
        Assert.Equal(3, Dependency.InstantiationCount);
    }

    private class Dependency
    {
        public static int InstantiationCount = 0;
        public Dependency()
        {
            InstantiationCount += 1;
        }
    }

    private class ActivatedSchemaTransformer : IOpenApiSchemaTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedSchemaTransformer()
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonTypeInfo.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiAny("1");
            }
            return Task.CompletedTask;
        }
    }

    private class ActivatedSchemaTransformerWithDependency: IOpenApiSchemaTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedSchemaTransformerWithDependency(Dependency dependency)
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class ActivatedOperationTransformer : IOpenApiOperationTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedOperationTransformer()
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            operation.Description = "Operation Description";
            return Task.CompletedTask;
        }
    }

    private class ActivatedOperationTransformerWithDependency: IOpenApiOperationTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedOperationTransformerWithDependency(Dependency dependency)
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class ActivatedDocumentTransformer : IOpenApiDocumentTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedDocumentTransformer()
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }

    private class ActivatedDocumentTransformerWithDependency : IOpenApiDocumentTransformer
    {
        public static int InstantiationCount = 0;
        public ActivatedDocumentTransformerWithDependency(Dependency dependency)
        {
            InstantiationCount += 1;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
