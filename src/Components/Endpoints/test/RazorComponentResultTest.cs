// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentResultTest
{
    [Fact]
    public void AcceptsNullParameters()
    {
        var result = new RazorComponentResult(typeof(SimpleComponent), null);
        Assert.NotNull(result.Parameters);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void AcceptsDictionaryParameters()
    {
        var paramsDict = new Dictionary<string, object> { { "First", 123 } };
        var result = new RazorComponentResult(typeof(SimpleComponent), paramsDict);
        Assert.Equal(1, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["First"]);
        Assert.Same(paramsDict, result.Parameters);
    }

    [Fact]
    public void AcceptsObjectParameters()
    {
        var result = new RazorComponentResult(typeof(SimpleComponent), new { Param1 = 123, Param2 = "Another" });
        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["Param1"]);
        Assert.Equal("Another", result.Parameters["Param2"]);
    }

    [Fact]
    public async Task CanRenderComponentStatically()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>();
        var httpContext = RazorComponentResultExecutorTest.GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
        Assert.Equal("text/html; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ResponseIncludesStatusCodeAndContentTypeAndHtml()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>
        {
            StatusCode = 123,
            ContentType = "application/test-content-type",
        };
        var httpContext = RazorComponentResultExecutorTest.GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
        Assert.Equal("application/test-content-type", httpContext.Response.ContentType);
        Assert.Equal(123, httpContext.Response.StatusCode);
    }

    private static string GetStringContent(MemoryStream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }
}
