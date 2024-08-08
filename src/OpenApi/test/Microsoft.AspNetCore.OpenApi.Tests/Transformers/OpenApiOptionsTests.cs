// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class OpenApiOptionsTests
{
    [Fact]
    public void AddDocumentTransformer_WithDocumentTransformerDelegate()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task>((document, context, cancellationToken) =>
        {
            document.Info.Title = "New Title";
            return Task.CompletedTask;
        });

        // Act
        var result = options.AddDocumentTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.IsType<DelegateOpenApiDocumentTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.OperationTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddDocumentTransformer_WithDocumentTransformerInstance()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new TestOpenApiDocumentTransformer();

        // Act
        var result = options.AddDocumentTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.Same(transformer, insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.OperationTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddDocumentTransformer_WithDocumentTransformerType()
    {
        // Arrange
        var options = new OpenApiOptions();

        // Act
        var result = options.AddDocumentTransformer<TestOpenApiDocumentTransformer>();

        // Assert
        var insertedTransformer = Assert.Single(options.DocumentTransformers);
        Assert.IsType<TypeBasedOpenApiDocumentTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.OperationTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddOperationTransformer_WithOperationTransformerDelegate()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task>((operation, context, cancellationToken) =>
        {
            operation.Description = "New Description";
            return Task.CompletedTask;
        });

        // Act
        var result = options.AddOperationTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.OperationTransformers);
        Assert.IsType<DelegateOpenApiOperationTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddOperationTransformer_WithOperationTransformerInstance()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new TestOpenApiOperationTransformer();

        // Act
        var result = options.AddOperationTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.OperationTransformers);
        Assert.Same(transformer, insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddOperationTransformer_WithOperationTransformerType()
    {
        // Arrange
        var options = new OpenApiOptions();

        // Act
        var result = options.AddOperationTransformer<TestOpenApiOperationTransformer>();

        // Assert
        var insertedTransformer = Assert.Single(options.OperationTransformers);
        Assert.IsType<TypeBasedOpenApiOperationTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.SchemaTransformers);
    }

    [Fact]
    public void AddSchemaTransformer_WithSchemaTransformerDelegate()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task>((schema, context, cancellationToken) =>
        {
            schema.Description = "New Description";
            return Task.CompletedTask;
        });

        // Act
        var result = options.AddSchemaTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.SchemaTransformers);
        Assert.IsType<DelegateOpenApiSchemaTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.OperationTransformers);
    }

    [Fact]
    public void AddSchemaTransformer_WithSchemaTransformerInstance()
    {
        // Arrange
        var options = new OpenApiOptions();
        var transformer = new TestOpenApiSchemaTransformer();

        // Act
        var result = options.AddSchemaTransformer(transformer);

        // Assert
        var insertedTransformer = Assert.Single(options.SchemaTransformers);
        Assert.Same(transformer, insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.OperationTransformers);
    }

    [Fact]
    public void AddSchemaTransformer_WithSchemaTransformerType()
    {
        // Arrange
        var options = new OpenApiOptions();

        // Act
        var result = options.AddSchemaTransformer<TestOpenApiSchemaTransformer>();

        // Assert
        var insertedTransformer = Assert.Single(options.SchemaTransformers);
        Assert.IsType<TypeBasedOpenApiSchemaTransformer>(insertedTransformer);
        Assert.IsType<OpenApiOptions>(result);
        Assert.Empty(options.DocumentTransformers);
        Assert.Empty(options.OperationTransformers);
    }

    private class TestOpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TestOpenApiOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TestOpenApiSchemaTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
