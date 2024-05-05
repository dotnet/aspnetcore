// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiOptionsTests
{
    [Fact]
    public void UseTransformer_WithDocumentTransformerDelegate()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task>((document, context, cancellationToken) =>
        {
            document.Info.Title = "New Title";
            return Task.CompletedTask;
        });

        // Act
        var result = options.UseTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.IsType<DelegateOpenApiDocumentTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
    }

    [Fact]
    public void UseTransformer_WithDocumentTransformerInstance()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new TestOpenApiDocumentTransformer();

        // Act
        var result = options.UseTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.Same(transformer, insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
    }

    [Fact]
    public void UseTransformer_WithDocumentTransformerType()
    {
        // Arrange
        var options = new OpenApiOptions();

        // Act
        var result = options.UseTransformer<TestOpenApiDocumentTransformer>();

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.IsType<TypeBasedOpenApiDocumentTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
    }

    [Fact]
    public void UseTransformer_WithOperationTransformerDelegate()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task>((operation, context, cancellationToken) =>
        {
            operation.Description = "New Description";
            return Task.CompletedTask;
        });

        // Act
        var result = options.UseOperationTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.IsType<DelegateOpenApiDocumentTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
    }

    private class TestOpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
