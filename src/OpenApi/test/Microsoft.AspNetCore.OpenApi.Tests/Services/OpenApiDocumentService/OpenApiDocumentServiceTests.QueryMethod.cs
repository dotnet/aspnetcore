// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    #nullable enable
    private record TodoItem(int Id, string Title, bool Completed);
#nullable restore
}
