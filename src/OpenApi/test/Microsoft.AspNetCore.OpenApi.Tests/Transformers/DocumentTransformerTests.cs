// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public class DocumentTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task DocumentTransformer_RunsInRegisteredOrder()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Description = "1";
            return Task.CompletedTask;
        });
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            Assert.Equal("1", document.Info.Description);
            document.Info.Description = "2";
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("2", document.Info.Description);
        });
    }

    [Fact]
    public async Task DocumentTransformer_SupportsActivatedTransformers()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer<ActivatedTransformer>();

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("Info Description", document.Info.Description);
        });
    }

    [Fact]
    public async Task DocumentTransformer_SupportsInstanceTransformers()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer(new ActivatedTransformer());

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("Info Description", document.Info.Description);
        });
    }

    [Fact]
    public async Task DocumentTransformer_SupportsActivatedTransformerWithSingletonDependency()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer<ActivatedTransformerWithDependency>();

        // Assert that singleton dependency is only instantiated once
        // regardless of the number of requests.
        string description = null;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            description = document.Info.Description;
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), description);
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal(description, document.Info.Description);
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), description);
        });
    }

    [Fact]
    public async Task DocumentTransformer_SupportsActivatedTransformerWithTransientDependency()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer<ActivatedTransformerWithDependency>();

        // Assert that transient dependency is instantiated twice for each
        // request to the OpenAPI document.
        string description = null;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            description = document.Info.Description;
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), description);
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.NotEqual(description, document.Info.Description);
            Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), document.Info.Description);
        });
    }

    [Fact]
    public async Task DocumentTransformer_SupportsDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer<DisposableTransformer>();

        DisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("Info Description", document.Info.Description);
        });
        Assert.Equal(1, DisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task DocumentTransformer_SupportsAsyncDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer<AsyncDisposableTransformer>();

        AsyncDisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("Info Description", document.Info.Description);
        });
        Assert.Equal(1, AsyncDisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task DocumentTransformer_CanAccessSingletonServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddDocumentTransformer((document, context, cancellationToken) =>
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
        // for the entire lifetime of the application, even though the
        // document is requested twice.
        Assert.Equal(1, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task DocumentTransformer_CanAccessScopedServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });

        // Assert that the scoped dependency is instantiated twice, once for
        // each request to the document.
        Assert.Equal(2, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task DocumentTransformer_CanAccessTransientServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        // Assert that the transient dependency is instantiated twice, once for
        // each `GetRequiredService` call in the transformer.
        Assert.Equal(2, Dependency.InstantiationCount);
    }

    private class ActivatedTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }

    private class DisposableTransformer : IOpenApiDocumentTransformer, IDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public void Dispose()
        {
            Disposed = true;
            DisposeCount += 1;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }

    private class AsyncDisposableTransformer : IOpenApiDocumentTransformer, IAsyncDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            DisposeCount += 1;
            return ValueTask.CompletedTask;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Description = "Info Description";
            return Task.CompletedTask;
        }
    }

    private class ActivatedTransformerWithDependency(Dependency dependency) : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            dependency.TestMethod();
            document.Info.Description = Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture);
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
