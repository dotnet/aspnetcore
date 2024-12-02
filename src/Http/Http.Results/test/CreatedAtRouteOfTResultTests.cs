// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public partial class CreatedAtRouteOfTResultTests
{
    [Fact]
    public void CreatedAtRouteResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });
        var obj = new HttpValidationProblemDetails();
        var result = new CreatedAtRoute<HttpValidationProblemDetails>(routeValues, obj);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(StatusCodes.Status201Created, obj.Status);
        Assert.Equal(obj, result.Value);
    }
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
        var linkGenerator = new TestLinkGenerator { Url = expectedUrl };
        var httpContext = GetHttpContext(linkGenerator);

        // Act
        var result = new CreatedAtRoute<object>(routeName: null, routeValues: values, value: null);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
        Assert.Equal(new RouteValueDictionary(values), linkGenerator.RouteValuesAddress.ExplicitValues);
    }

    [Fact]
    public async Task CreatedAtRouteResult_ThrowsOnNullUrl()
    {
        // Arrange
        var linkGenerator = new TestLinkGenerator();
        var httpContext = GetHttpContext(linkGenerator);

        var result = new CreatedAtRoute<object>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            async () => await result.ExecuteAsync(httpContext),
        "No route matches the supplied values.");
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        CreatedAtRoute<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<CreatedAtRoute<Todo>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status201Created, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new CreatedAtRoute<object>(null, null);
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<CreatedAtRoute<object>>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<CreatedAtRoute<object>>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void CreatedAtRouteResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Arrange & Act
        var rawResult = new CreatedAtRoute<object>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(rawResult);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public void CreatedAtRouteResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange & Act
        var value = "Foo";
        var rawResult = new CreatedAtRoute<string>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: value);

        // Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(rawResult);
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRouteResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange & Act
        var value = "Foo";
        var rawResult = new CreatedAtRoute<string>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: value);

        // Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(rawResult);
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private record Todo(int Id, string Title);

    private static HttpContext GetHttpContext(LinkGenerator linkGenerator)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices(linkGenerator);
        return httpContext;
    }

    private static IServiceProvider CreateServices(LinkGenerator linkGenerator)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<LinkGenerator>(linkGenerator);

        return services.BuildServiceProvider();
    }
}
