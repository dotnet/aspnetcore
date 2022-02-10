// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Result;

public partial class CreatedAtRouteResultTests
{
    public static IEnumerable<object[]> CreatedAtRouteData
    {
        get
        {
            yield return new object[] { null };
            yield return
                new object[] {
                        new Dictionary<string, string>() { { "hello", "world" } }
                };
            yield return
                new object[] {
                        new RouteValueDictionary(new Dictionary<string, string>() {
                            { "test", "case" },
                            { "sample", "route" }
                        })
                };
        }
    }

    [Theory]
    [MemberData(nameof(CreatedAtRouteData))]
    public async Task CreatedAtRouteResult_ReturnsStatusCode_SetsLocationHeader(object values)
    {
        // Arrange
        var expectedUrl = "testAction";
        var httpContext = GetHttpContext(expectedUrl);

        // Act
        var result = new CreatedAtRouteResult(routeName: null, routeValues: values, value: null);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task CreatedAtRouteResult_ThrowsOnNullUrl()
    {
        // Arrange
        var httpContext = GetHttpContext(expectedUrl: null);

        var result = new CreatedAtRouteResult(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            async () => await result.ExecuteAsync(httpContext),
        "No route matches the supplied values.");
    }

    private static HttpContext GetHttpContext(string expectedUrl)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices(expectedUrl);
        return httpContext;
    }

    private static IServiceProvider CreateServices(string expectedUrl)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<LinkGenerator>(new TestLinkGenerator
        {
            Url = expectedUrl
        });

        return services.BuildServiceProvider();
    }
}
