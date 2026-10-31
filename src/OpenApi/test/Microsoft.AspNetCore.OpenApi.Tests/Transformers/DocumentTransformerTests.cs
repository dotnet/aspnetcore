// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task DocumentTransformer_RunsBeforeOperationGenerationToPreserveTagMetadata()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { }).WithTags("todos", "v1");

        var options = new OpenApiOptions();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Tags ??= new SortedSet<OpenApiTag>(Comparer<OpenApiTag>.Create(
                static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name)));
            document.Tags.Add(new OpenApiTag
            {
                Name = "todos",
                Description = "Operations for managing todo items."
            });
            document.Tags.Add(new OpenApiTag
            {
                Name = "v1",
                Description = "Version 1 operations."
            });
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) => Task.CompletedTask);

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Collection(document.Tags,
                tag =>
                {
                    Assert.Equal("todos", tag.Name);
                    Assert.Equal("Operations for managing todo items.", tag.Description);
                },
                tag =>
                {
                    Assert.Equal("v1", tag.Name);
                    Assert.Equal("Version 1 operations.", tag.Description);
                });
        });
    }

    [Fact]
    public async Task DocumentTransformer_RunsBeforeOperationTransformerWhenRegisteredFirst()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Description = "Document transformer ran.";
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("Document transformer ran.", context.Document.Info.Description);
            operation.Description = "Operation transformer ran.";
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("Document transformer ran.", document.Info.Description);
            Assert.Equal("Operation transformer ran.", document.Paths["/todo"].Operations[HttpMethod.Get].Description);
        });
    }

    [Fact]
    public async Task TransformerRegisteredAsOperationTransformer_DoesNotRunAsDocumentTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });

        var transformer = new OperationAndDocumentTransformer();
        var options = new OpenApiOptions();
        options.AddOperationTransformer(transformer);

        await VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Null(document.Info.Description);
            Assert.Equal("Operation transformer ran.", document.Paths["/todo"].Operations[HttpMethod.Get].Description);
        });

        Assert.Equal(0, transformer.DocumentTransformCount);
        Assert.Equal(1, transformer.OperationTransformCount);
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

    [Fact]
    public async Task DocumentTransformer_RespectsOperationCancellation()
    {
        var builder = CreateBuilder();
        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        var transformerCalled = false;
        var exceptionThrown = false;
        var tcs = new TaskCompletionSource();

        options.AddDocumentTransformer(async (document, context, cancellationToken) =>
        {
            transformerCalled = true;
            try
            {
                await tcs.Task.WaitAsync(cancellationToken);
                document.Info.Description = "Should not be set";
            }
            catch (OperationCanceledException)
            {
                exceptionThrown = true;
                throw;
            }
        });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1);

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await VerifyOpenApiDocument(builder, options, _ => { }, cts.Token);
        });

        Assert.True(transformerCalled);
        Assert.True(exceptionThrown);
    }

    [Fact]
    public async Task DocumentTransformer_ExecutesAsynchronously()
    {
        var builder = CreateBuilder();
        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        var transformerOrder = new List<int>();
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        options.AddDocumentTransformer(async (document, context, cancellationToken) =>
        {
            await tcs1.Task;
            transformerOrder.Add(1);
            document.Info.Title = "First";
        });

        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            transformerOrder.Add(2);
            document.Info.Title += " Second";
            tcs2.TrySetResult();
            return Task.CompletedTask;
        });

        options.AddDocumentTransformer(async (document, context, cancellationToken) =>
        {
            await tcs2.Task;
            transformerOrder.Add(3);
            document.Info.Title += " Third";
        });

        var documentTask = VerifyOpenApiDocument(builder, options, document =>
        {
            Assert.Equal("First Second Third", document.Info.Title);
        });

        tcs1.TrySetResult();

        await documentTask;

        Assert.Equal([1, 2, 3], transformerOrder);
    }

    private sealed class OperationAndDocumentTransformer : IOpenApiOperationTransformer, IOpenApiDocumentTransformer
    {
        public int OperationTransformCount { get; private set; }

        public int DocumentTransformCount { get; private set; }

        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            OperationTransformCount++;
            operation.Description = "Operation transformer ran.";
            return Task.CompletedTask;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            DocumentTransformCount++;
            document.Info.Description = "Document transformer ran.";
            return Task.CompletedTask;
        }
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
