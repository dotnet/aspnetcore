// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiParameters_GeneratesParameterLocationCorrectly()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos/{id}", (int id) => { });
        builder.MapGet("/api/todos", (int id) => { });
        builder.MapGet("/api", ([FromHeader(Name = "X-Header")] string header) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var pathParameter = Assert.Single(document.Paths["/api/todos/{id}"].Operations[OperationType.Get].Parameters);
            Assert.Equal("id", pathParameter.Name);
            Assert.Equal(ParameterLocation.Path, pathParameter.In);

            var queryParameter = Assert.Single(document.Paths["/api/todos"].Operations[OperationType.Get].Parameters);
            Assert.Equal("id", queryParameter.Name);
            Assert.Equal(ParameterLocation.Query, queryParameter.In);

            var headerParameter = Assert.Single(document.Paths["/api"].Operations[OperationType.Get].Parameters);
            Assert.Equal("X-Header", headerParameter.Name);
            Assert.Equal(ParameterLocation.Header, headerParameter.In);
        });
    }

#nullable enable
    [Fact]
    public async Task GetOpenApiParameters_RouteParametersAreAlwaysRequired()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos/{id}", (int id) => { });
        builder.MapGet("/api/todos/{guid}", (Guid? guid) => { });
        builder.MapGet("/api/todos/{isCompleted}", (bool isCompleted = false) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var pathParameter = Assert.Single(document.Paths["/api/todos/{id}"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("id", pathParameter.Name);
            Assert.True(pathParameter.Required);
            var guidParameter = Assert.Single(document.Paths["/api/todos/{guid}"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("guid", guidParameter.Name);
            Assert.True(guidParameter.Required);
            var isCompletedParameter = Assert.Single(document.Paths["/api/todos/{isCompleted}"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("isCompleted", isCompletedParameter.Name);
            Assert.True(isCompletedParameter.Required);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_SetsRequirednessForQueryParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", (int id) => { });
        builder.MapGet("/api/users", (int? id) => { });
        builder.MapGet("/api/projects", (int id = 1) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var queryParameter = Assert.Single(document.Paths["/api/todos"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("id", queryParameter.Name);
            Assert.True(queryParameter.Required);
            var nullableQueryParameter = Assert.Single(document.Paths["/api/users"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("id", nullableQueryParameter.Name);
            Assert.False(nullableQueryParameter.Required);
            var defaultQueryParameter = Assert.Single(document.Paths["/api/projects"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("id", defaultQueryParameter.Name);
            Assert.False(defaultQueryParameter.Required);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_SetsRequirednessForHeaderParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos", ([FromHeader(Name = "X-Header")] string header) => { });
        builder.MapGet("/api/users", ([FromHeader(Name = "X-Header")] Guid? header) => { });
        builder.MapGet("/api/projects", ([FromHeader(Name = "X-Header")] string header = "0000-0000-0000-0000") => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var headerParameter = Assert.Single(document.Paths["/api/todos"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("X-Header", headerParameter.Name);
            Assert.True(headerParameter.Required);
            var nullableHeaderParameter = Assert.Single(document.Paths["/api/users"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("X-Header", nullableHeaderParameter.Name);
            Assert.False(nullableHeaderParameter.Required);
            var defaultHeaderParameter = Assert.Single(document.Paths["/api/projects"].Operations[OperationType.Get].Parameters!);
            Assert.Equal("X-Header", defaultHeaderParameter.Name);
            Assert.False(defaultHeaderParameter.Required);
        });
    }
#nullable restore

// Test coverage for https://github.com/dotnet/aspnetcore/issues/46746 requires disabling nullability
#nullable disable
    [Fact]
    public async Task GetOpenApiParameters_RouteParametersAreAlwaysRequired_NullabilityDisabled()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/todos/{id}", (int id) => { });
        builder.MapGet("/api/todos/{guid}", (Guid? guid) => { });
        builder.MapGet("/api/todos/{isCompleted}", (bool isCompleted = false) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var pathParameter = Assert.Single(document.Paths["/api/todos/{id}"].Operations[OperationType.Get].Parameters);
            Assert.Equal("id", pathParameter.Name);
            Assert.True(pathParameter.Required);
            var guidParameter = Assert.Single(document.Paths["/api/todos/{guid}"].Operations[OperationType.Get].Parameters);
            Assert.Equal("guid", guidParameter.Name);
            Assert.True(guidParameter.Required);
            var isCompletedParameter = Assert.Single(document.Paths["/api/todos/{isCompleted}"].Operations[OperationType.Get].Parameters);
            Assert.Equal("isCompleted", isCompletedParameter.Name);
            Assert.True(isCompletedParameter.Required);
        });
    }
#nullable restore

    [Fact]
    public async Task GetOpenApiRequestBody_SkipsRequestBodyParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api/users", (IFormFile formFile, IFormFileCollection formFiles) => { });
        builder.MapPost("/api/todos", (Todo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var usersOperation = document.Paths["/api/users"].Operations[OperationType.Post];
            Assert.Null(usersOperation.Parameters);
            var todosOperation = document.Paths["/api/todos"].Operations[OperationType.Post];
            Assert.Null(todosOperation.Parameters);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_SkipsDisallowedHeaders()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/accept", ([FromHeader(Name = "Accept")] string value) => { });
        builder.MapGet("/api/accept-lower", ([FromHeader(Name = "accept")] string value) => { });
        builder.MapGet("/api/authorization", ([FromHeader(Name = "Authorization")] string value) => { });
        builder.MapGet("/api/authorization-lower", ([FromHeader(Name = "authorization")] string value) => { });
        builder.MapGet("/api/content-type", ([FromHeader(Name = "Content-Type")] string value) => { });
        builder.MapGet("/api/content-type-lower", ([FromHeader(Name = "content-type")] string value) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            Assert.Null(document.Paths["/api/accept"].Operations[OperationType.Get].Parameters);
            Assert.Null(document.Paths["/api/accept-lower"].Operations[OperationType.Get].Parameters);
            Assert.Null(document.Paths["/api/authorization"].Operations[OperationType.Get].Parameters);
            Assert.Null(document.Paths["/api/authorization-lower"].Operations[OperationType.Get].Parameters);
            Assert.Null(document.Paths["/api/content-type"].Operations[OperationType.Get].Parameters);
            Assert.Null(document.Paths["/api/content-type-lower"].Operations[OperationType.Get].Parameters);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_ToleratesCustomBindingSource()
    {
        var action = CreateActionDescriptor(nameof(ActionWithCustomBinder));

        await VerifyOpenApiDocument(action, document =>
        {
            var operation = document.Paths["/custom-binding"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal("model", parameter.Name);
            Assert.Equal(ParameterLocation.Query, parameter.In);
        });
    }

    [Route("/custom-binding")]
    private void ActionWithCustomBinder([ModelBinder(BinderType = typeof(CustomBinder))] Todo model) { }

    public class CustomBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return Task.CompletedTask;
        }
    }
}
