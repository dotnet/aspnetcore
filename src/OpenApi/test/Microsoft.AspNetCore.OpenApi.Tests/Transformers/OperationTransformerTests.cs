// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public class OperationTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task OperationTransformer_CanAccessApiDescription()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            var apiDescription = context.Description;
            operation.Description = apiDescription.RelativePath;
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("todo", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("user", operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformers_RunInRegisteredOrder()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();

        // While added first, document transformers should run after the operation transformers
        options.AddDocumentTransformer<MyDocumentationTransformer>();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            Assert.All(document.Paths.Values.SelectMany(p => p.Operations).Select(p => p.Value), o => Assert.Equal("6", o.Description));
            return Task.CompletedTask;
        });

        // Operation transforms should run FIFO regardless of which kind of transformer is used
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Null(operation.Description);
            operation.Description = "1";
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("1", operation.Description);
            operation.Description = "2";
            return Task.CompletedTask;
        });
        options.AddOperationTransformer<MyOperationTransformer3>();
        options.AddOperationTransformer(new MyOperationTransformer4());
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("4", operation.Description);
            operation.Description = "5";
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("5", operation.Description);
            operation.Description = "6";
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("6", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("6", operation.Description);
                });
        });
    }

    private sealed class MyDocumentationTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            Assert.All(document.Paths.Values.SelectMany(p => p.Operations).Select(p => p.Value), o => Assert.Equal("6", o.Description));
            return Task.CompletedTask;
        }
    }

    private sealed class MyOperationTransformer3 : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            Assert.Equal("2", operation.Description);
            operation.Description = "3";
            return Task.CompletedTask;
        }
    }

    private sealed class MyOperationTransformer4 : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            Assert.Equal("3", operation.Description);
            operation.Description = "4";
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task OperationTransformer_CanMutateOperationViaDocumentTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            foreach (var pathItem in document.Paths.Values)
            {
                foreach (var operation in pathItem.Operations.Values)
                {
                    operation.Description = "3";
                }
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("3", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("3", operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformer_CanMutateOperationViaOperationTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Description = "3";
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("3", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("3", operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformer_SupportsActivatedTransformers()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer<ActivatedTransformer>();

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformer_SupportsInstanceTransformers()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer(new ActivatedTransformer());

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformer_SupportsActivatedTransformerWithSingletonDependency()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer<ActivatedTransformerWithDependency>();

        // Assert that singleton dependency is only instantiated once
        // regardless of the number of requests and operations.
        string description = null;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    description = operation.Description;
                    Assert.Equal(Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture), description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal(description, operation.Description);
                });
        });
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal(description, operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal(description, operation.Description);
                });
        });
    }

    [Fact]
    public async Task OperationTransformer_SupportsActivatedTransformerWithTransientDependency()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer<ActivatedTransformerWithDependency>();

        Dependency.InstantiationCount = 0;
        await VerifyOpenApiDocument(builder, options, document => { });
        await VerifyOpenApiDocument(builder, options, document => { });
        // Assert that transient dependency has "scoped" lifetime and is instantiated
        // once for each request to the document.
        Assert.Equal(2, Dependency.InstantiationCount);
    }

    [Fact]
    public async Task OperationTransformer_SupportsDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer<DisposableTransformer>();

        DisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                });
        });
        Assert.Equal(1, DisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task OperationTransformer_SupportsAsyncDisposableActivatedTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.AddOperationTransformer<AsyncDisposableTransformer>();

        AsyncDisposableTransformer.DisposeCount = 0;
        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/todo", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                },
                path =>
                {
                    Assert.Equal("/user", path.Key);
                    var operation = Assert.Single(path.Value.Operations.Values);
                    Assert.Equal("Operation Description", operation.Description);
                });
        });
        Assert.Equal(1, AsyncDisposableTransformer.DisposeCount);
    }

    [Fact]
    public async Task OperationTransformer_CanAccessSingletonServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddSingleton<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddOperationTransformer((operation, context, cancellationToken) =>
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
    public async Task OperationTransformer_CanAccessScopedServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddScoped<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddOperationTransformer((operation, context, cancellationToken) =>
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
    public async Task OperationTransformer_CanAccessTransientServiceFromContextApplicationServices()
    {
        var serviceCollection = new ServiceCollection().AddTransient<Dependency>();
        var builder = CreateBuilder(serviceCollection);

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));
        builder.MapGet("/todo-2", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        Dependency.InstantiationCount = 0;
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetRequiredService<Dependency>();
            var sameServiceAgain = context.ApplicationServices.GetRequiredService<Dependency>();
            service.TestMethod();
            sameServiceAgain.TestMethod();
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
        // Assert that the transient dependency is instantiated four times, twice
        // within the operation transformer that is invoked for two operations.
        Assert.Equal(4, Dependency.InstantiationCount);
    }

    private class ActivatedTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            operation.Description = "Operation Description";
            return Task.CompletedTask;
        }
    }

    private class DisposableTransformer : IOpenApiOperationTransformer, IDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public void Dispose()
        {
            Disposed = true;
            DisposeCount += 1;
        }

        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            operation.Description = "Operation Description";
            return Task.CompletedTask;
        }
    }

    private class AsyncDisposableTransformer : IOpenApiOperationTransformer, IAsyncDisposable
    {
        internal bool Disposed = false;
        internal static int DisposeCount = 0;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            DisposeCount += 1;
            return ValueTask.CompletedTask;
        }

        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            operation.Description = "Operation Description";
            return Task.CompletedTask;
        }
    }

    private class ActivatedTransformerWithDependency(Dependency dependency) : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            dependency.TestMethod();
            operation.Description = Dependency.InstantiationCount.ToString(CultureInfo.InvariantCulture);
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
