// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

public class OperationTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task OperationTransformer_CanAccessApiDescription()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.UseOperationTransformer((operation, context, cancellationToken) =>
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
    public async Task OperationTransformer_RunsInRegisteredOrder()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Description = "1";
            return Task.CompletedTask;
        });
        options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("1", operation.Description);
            operation.Description = "2";
            return Task.CompletedTask;
        });
        options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            Assert.Equal("2", operation.Description);
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
    public async Task OperationTransformer_CanMutateOperationViaDocumentTransformer()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });
        builder.MapGet("/user", () => { });

        var options = new OpenApiOptions();
        options.UseTransformer((document, context, cancellationToken) =>
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
    public async Task OperationTransformer_ThrowsExceptionIfDescriptionIdNotFound()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => { });

        var options = new OpenApiOptions();
        options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Extensions.Remove("x-aspnetcore-id");
            return Task.CompletedTask;
        });
        options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            return Task.CompletedTask;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => VerifyOpenApiDocument(builder, options, _ => { }));
        Assert.Equal("Cached operation transformer context not found. Please ensure that the operation contains the `x-aspnetcore-id` extension attribute.", exception.Message);
    }
}
