// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

public partial class OpenApiDocumentServiceTests
{
    [Fact]
    public async Task GetOpenApiOperation_CapturesSummary()
    {
        // Arrange
        var builder = CreateBuilder();
        var summary = "Get all todos";

        // Act
        builder.MapGet("/api/todos", () => { }).WithSummary(summary);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal(summary, operation.Summary);
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesLastSummary()
    {
        // Arrange
        var builder = CreateBuilder();
        var summary = "Get all todos";

        // Act
        builder.MapGet("/api/todos", () => { }).WithSummary(summary).WithSummary(summary + "1");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal(summary + "1", operation.Summary);
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesDescription()
    {
        // Arrange
        var builder = CreateBuilder();
        var description = "Returns all the todos provided in an array.";

        // Act
        builder.MapGet("/api/todos", () => { }).WithDescription(description);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal(description, operation.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesDescriptionLastDescription()
    {
        // Arrange
        var builder = CreateBuilder();
        var description = "Returns all the todos provided in an array.";

        // Act
        builder.MapGet("/api/todos", () => { }).WithDescription(description).WithDescription(description + "1");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal(description + "1", operation.Description);
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesTags()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { }).WithTags(["todos", "v1"]);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Collection(operation.Tags, tag =>
            {
                Assert.Equal("todos", tag.Name);
            },
            tag =>
            {
                Assert.Equal("v1", tag.Name);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesTagsLastTags()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { }).WithTags(["todos", "v1"]).WithTags(["todos", "v2"]);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Collection(operation.Tags, tag =>
            {
                Assert.Equal("todos", tag.Name);
            },
            tag =>
            {
                Assert.Equal("v2", tag.Name);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_SetsDefaultValueForTags()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Collection(document.Tags, tag =>
            {
                Assert.Equal(nameof(OpenApiDocumentServiceTests), tag.Name);
            });
            Assert.Collection(operation.Tags, tag =>
            {
                Assert.Equal(nameof(OpenApiDocumentServiceTests), tag.Name);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesTagsInDocument()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { }).WithTags(["todos", "v1"]);
        builder.MapGet("/api/users", () => { }).WithTags(["users", "v1"]);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Tags, tag =>
            {
                Assert.Equal("todos", tag.Name);
            },
            tag =>
            {
                Assert.Equal("v1", tag.Name);
            },
            tag =>
            {
                Assert.Equal("users", tag.Name);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesEndpointNameAsOperationId()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { }).WithName("GetTodos");

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal("GetTodos", operation.OperationId);

        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesEndpointNameAttributeAsOperationId()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", [EndpointName("GetTodos")] () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal("GetTodos", operation.OperationId);

        });
    }

    [Fact]
    public async Task GetOpenApiOperation_CapturesRouteAttributeAsOperationId()
    {
        // Act
        var action = CreateActionDescriptor(nameof(ActionWithRouteAttributeName));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = document.Paths["/api/todos"].Operations[OperationType.Get];
            Assert.Equal("GetTodos", operation.OperationId);

        });
    }

    [Route("/api/todos", Name = "GetTodos")]
    private void ActionWithRouteAttributeName() { }
}
