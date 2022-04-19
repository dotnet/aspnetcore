// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class AcceptedAtRouteResultTests
{
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
        var result = new AcceptedAtRoute(routeValues: values);
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
        var result = new AcceptedAtRoute(
            routeName: null,
            routeValues: new Dictionary<string, object>());

        // Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(() =>
            result.ExecuteAsync(httpContext),
            "No route matches the supplied values.");
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        AcceptedAtRoute MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        // Act
        PopulateMetadata<AcceptedAtRoute>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status202Accepted, producesResponseTypeMetadata.StatusCode);
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

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
