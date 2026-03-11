// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

public partial class OpenApiDocumentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task QueryMethod_AppearsInDocument()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithQueryMethod));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            // Check that QUERY method operation exists
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];
            Assert.NotNull(operation);
        });
    }

    [Fact]
    public async Task QueryMethod_SupportsRequestBody()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ActionWithQueryMethodAndBody));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];

            // QUERY should support request bodies
            Assert.NotNull(operation.RequestBody);
            Assert.True(operation.RequestBody.Required);
            Assert.NotNull(operation.RequestBody.Content);
            var content = Assert.Single(operation.RequestBody.Content);
            Assert.Equal("application/json", content.Key);
        });
    }

    [Fact]
    public async Task QueryMethod_WithoutBody_StillWorks()
    {
        // Arrange - using minimal API approach for testing query parameters
        var builder = CreateBuilder();

        // Act
        builder.MapMethods("/api/search", [HttpMethods.Query], (string query) => Results.Ok(query));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            Assert.True(path.Operations.ContainsKey(HttpMethod.Query));
            var operation = path.Operations[HttpMethod.Query];

            // QUERY without body should have query parameters
            Assert.Null(operation.RequestBody);
            Assert.NotNull(operation.Parameters);
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(ParameterLocation.Query, parameter.In);
        });
    }

    public class HttpQuery : HttpMethodAttribute
    {
        public HttpQuery() : base(["QUERY"]) { }
    }

    [HttpQuery]
    [Route("/query")]
    private void ActionWithQueryMethod() { }

    [HttpQuery]
    [Route("/query-with-body")]
    private void ActionWithQueryMethodAndBody([FromBody] TodoItem todo) { }

#nullable enable
    private record TodoItem(int Id, string Title, bool Completed);
#nullable restore
}
