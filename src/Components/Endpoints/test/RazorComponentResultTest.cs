// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public async Task ResponseIncludesStatusCodeAndContentTypeAndHtml()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>
        {
            StatusCode = 123,
            ContentType = "application/test-content-type",
        };
        var httpContext = RazorComponentEndpointTest.GetTestHttpContext();
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

    class SimpleComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Hello from SimpleComponent");
            builder.CloseElement();
        }
    }
}
