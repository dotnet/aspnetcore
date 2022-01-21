// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Result;

public class AcceptedAtRouteResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var url = "testAction";
        var linkGenerator = new TestLinkGenerator { Url = url };
        var httpContext = GetHttpContext(linkGenerator);
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
            {
                { "test", "case" },
                { "sample", "route" }
            });

        // Act
        var result = new AcceptedAtRouteResult(
            routeName: "sample",
            routeValues: routeValues,
            value: "Hello world");
        await result.ExecuteAsync(httpContext);

        // Assert
        var response = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("\"Hello world\"", response);
    }

    public static TheoryData<object> AcceptedAtRouteData
    {
        get
        {
            return new TheoryData<object>
                {
                    null,
                    new Dictionary<string, string>()
                    {
                        { "hello", "world" }
                    },
                    new RouteValueDictionary(
                        new Dictionary<string, string>()
                        {
                            { "test", "case" },
                            { "sample", "route" }
                        }),
                    };
        }
    }

    [Theory]
    [MemberData(nameof(AcceptedAtRouteData))]
    public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader(object values)
    {
        // Arrange
        var expectedUrl = "testAction";
        var linkGenerator = new TestLinkGenerator { Url = expectedUrl };
        var httpContext = GetHttpContext(linkGenerator);

        // Act
        var result = new AcceptedAtRouteResult(routeValues: values, value: null);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_ThrowsIfRouteUrlIsNull()
    {
        // Arrange
        var linkGenerator = new TestLinkGenerator();
        var httpContext = GetHttpContext(linkGenerator);

        // Act
        var result = new AcceptedAtRouteResult(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(() =>
            result.ExecuteAsync(httpContext),
            "No route matches the supplied values.");
    }

    private static HttpContext GetHttpContext(LinkGenerator linkGenerator)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices(linkGenerator);
        return httpContext;
    }

    private static IServiceProvider CreateServices(LinkGenerator linkGenerator)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(linkGenerator);
        return services.BuildServiceProvider();
    }
}
