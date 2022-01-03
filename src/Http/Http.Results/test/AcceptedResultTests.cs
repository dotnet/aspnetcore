// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result;

public class AcceptedResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;
        // Act
        var result = new AcceptedResult("my-location", value: "Hello world");
        await result.ExecuteAsync(httpContext);

        // Assert
        var response = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("\"Hello world\"", response);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
    {
        // Arrange
        var expectedUrl = "testAction";
        var httpContext = GetHttpContext();

        // Act
        var result = new AcceptedResult(expectedUrl, value: "some-value");
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services.BuildServiceProvider();
    }
}
