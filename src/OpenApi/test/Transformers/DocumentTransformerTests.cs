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
        options.UseTransformer((document, context, cancellationToken) =>
        {
            document.Info.Description = "1";
            return Task.CompletedTask;
        });
        options.UseTransformer((document, context, cancellationToken) =>
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
        options.UseTransformer<ActivatedTransformer>();

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
        options.UseTransformer(new ActivatedTransformer());

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
        options.UseTransformer<ActivatedTransformerWithDependency>();

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
        options.UseTransformer<ActivatedTransformerWithDependency>();

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

    private class ActivatedTransformer : IOpenApiDocumentTransformer
    {
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
