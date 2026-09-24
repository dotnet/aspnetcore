// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task QueryMethod_AppearsInDocument()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapMethods("/api/search", [HttpMethods.Query], () => Results.Ok());

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];
            Assert.NotNull(operation);
        });
    }

    [Fact]
    public async Task QueryMethod_SupportsRequestBody()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapMethods("/api/search", [HttpMethods.Query], (TodoItem todo) => Results.Ok(todo));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json", content.Key);
        });
    }

    [Fact]
    public async Task QueryMethod_WithQueryParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapMethods("/api/search", [HttpMethods.Query], (string query) => Results.Ok(query));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];
            Assert.Null(operation.RequestBody);
            Assert.NotNull(operation.Parameters);
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(ParameterLocation.Query, parameter.In);
        });
    }

    [Fact]
    public async Task CustomMethod_AppearsInDocumentForMvcAction()
    {
        var action = CreateActionDescriptor(nameof(ActionWithCustomMethod));

        await VerifyOpenApiDocument(action, document =>
        {
            var path = document.Paths["/api/custom"];
            Assert.True(path.Operations.ContainsKey(new HttpMethod("FOO")));
            Assert.DoesNotContain(HttpMethod.Post, path.Operations.Keys);
        });
    }

    [Fact]
    public async Task CustomMethod_IsVisitedByForEachOperationAsync()
    {
        var builder = CreateBuilder();
        var action = CreateActionDescriptor(nameof(ActionWithCustomMethod));
        var documentService = CreateDocumentService(builder, action);
        using var scopedService = builder.ServiceProvider.CreateScope();
        var document = await documentService.GetOpenApiDocumentAsync(scopedService.ServiceProvider);

        await documentService.ForEachOperationAsync(document, (operation, context, cancellationToken) =>
        {
            operation.Description = context.Description.HttpMethod;
            return Task.CompletedTask;
        }, CancellationToken.None);

        var operation = document.Paths["/api/custom"].Operations[new HttpMethod("FOO")];
        Assert.Equal("FOO", operation.Description);
    }

    [Theory]
    [InlineData(OpenApiSpecVersion.OpenApi3_0, OpenApiSpecVersion.OpenApi3_2)]
    [InlineData(OpenApiSpecVersion.OpenApi3_2, OpenApiSpecVersion.OpenApi3_0)]
    public async Task ForEachOperationAsync_UsesContextsFromSequentialGeneration(
        OpenApiSpecVersion firstVersion,
        OpenApiSpecVersion secondVersion)
    {
        var (documentService, firstServices, secondServices) = CreateVersionedDocumentService();
        using (firstServices)
        using (secondServices)
        {
            await documentService.GetOpenApiDocumentAsync(firstServices, httpRequest: null, firstVersion);
            await documentService.GetOpenApiDocumentAsync(secondServices, httpRequest: null, secondVersion);
        }
    }

    [Fact]
    public async Task ForEachOperationAsync_UsesContextsFromConcurrentGeneration()
    {
        var (documentService, firstServices, secondServices) = CreateVersionedDocumentService();
        using (firstServices)
        using (secondServices)
        {
            await Task.WhenAll(
                documentService.GetOpenApiDocumentAsync(firstServices, httpRequest: null, OpenApiSpecVersion.OpenApi3_0),
                documentService.GetOpenApiDocumentAsync(secondServices, httpRequest: null, OpenApiSpecVersion.OpenApi3_2));
        }
    }

    private static (OpenApiDocumentService DocumentService, ServiceProvider FirstServices, ServiceProvider SecondServices) CreateVersionedDocumentService()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api/versioned", () => Results.Ok());

        OpenApiDocumentService documentService = null;
        var options = new OpenApiOptions();
        options.AddSchemaTransformer((schema, context, cancellationToken) => Task.CompletedTask);
        options.AddDocumentTransformer(async (document, context, cancellationToken) =>
        {
            await documentService.ForEachOperationAsync(document, (operation, operationContext, _) =>
            {
#pragma warning disable ASP0040 // Test exercises the experimental transformer version.
                Assert.Equal(context.OpenApiVersion, operationContext.OpenApiVersion);
#pragma warning restore ASP0040
                Assert.Same(document, operationContext.Document);
                Assert.Same(context.ApplicationServices, operationContext.ApplicationServices);
                Assert.Same(context.SchemaTransformers, operationContext.SchemaTransformers);
                return Task.CompletedTask;
            }, cancellationToken);
        });
        documentService = CreateDocumentService(builder, options);

        return (
            documentService,
            new ServiceCollection().BuildServiceProvider(),
            new ServiceCollection().BuildServiceProvider());
    }

    #nullable enable
    private record TodoItem(int Id, string Title, bool Completed);
#nullable restore

    [Route("/api/custom")]
    [HttpFoo]
    private ActionResult<TodoItem> ActionWithCustomMethod()
        => new OkObjectResult(new TodoItem(100, "Title", true));

    private sealed class HttpFooAttribute() : HttpMethodAttribute(["FOO"]);
}
