// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiPaths_ReturnsPaths()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { });
        builder.MapGet("/api/users", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/api/todos", path.Key);
                    Assert.Collection(path.Value.Operations.OrderBy(o => o.Key),
                        operation =>
                        {
                            Assert.Equal(OperationType.Get, operation.Key);
                        });
                },
                path =>
                {
                    Assert.Equal("/api/users", path.Key);
                    Assert.Collection(path.Value.Operations.OrderBy(o => o.Key),
                        operation =>
                        {
                            Assert.Equal(OperationType.Get, operation.Key);
                        });
                });
        });
    }

    [Fact]
    public async Task GetOpenApiPaths_RespectsShouldInclude()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { }).WithMetadata(new EndpointGroupNameAttribute("v1"));
        builder.MapGet("/api/users", () => { }).WithMetadata(new EndpointGroupNameAttribute("v2"));

        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/api/todos", path.Key);
                }
            );
        });
    }

    [Fact]
    public async Task GetOpenApiPaths_RespectsSamePaths()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", () => { });
        builder.MapPost("/api/todos", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/api/todos", path.Key);
                    Assert.Collection(path.Value.Operations.OrderBy(o => o.Key),
                        operation =>
                        {
                            Assert.Equal(OperationType.Get, operation.Key);
                        },
                        operation =>
                        {
                            Assert.Equal(OperationType.Post, operation.Key);
                        });
                }
            );
        });
    }

    [Fact]
    public async Task GetOpenApiPaths_HandlesRouteParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos/{id}", () => { });
        builder.MapPost("/api/todos/{id}", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/api/todos/{id}", path.Key);
                    Assert.Collection(path.Value.Operations.OrderBy(o => o.Key),
                        operation =>
                        {
                            Assert.Equal(OperationType.Get, operation.Key);
                        },
                        operation =>
                        {
                            Assert.Equal(OperationType.Post, operation.Key);
                        });
                }
            );
        });
    }

    [Fact]
    public async Task GetOpenApiPaths_HandlesRouteConstraints()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos/{id:int}", () => { });
        builder.MapPost("/api/todos/{id:int}", () => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Collection(document.Paths.OrderBy(p => p.Key),
                path =>
                {
                    Assert.Equal("/api/todos/{id}", path.Key);
                    Assert.Collection(path.Value.Operations.OrderBy(o => o.Key),
                        operation =>
                        {
                            Assert.Equal(OperationType.Get, operation.Key);
                        },
                        operation =>
                        {
                            Assert.Equal(OperationType.Post, operation.Key);
                        });
                }
            );
        });
    }
}
