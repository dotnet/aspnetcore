// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class AcceptedAtRouteOfTResultTests
{
    [Fact]
    public void AcceptedAtRouteResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });
        var obj = new HttpValidationProblemDetails();
        var result = new AcceptedAtRoute<HttpValidationProblemDetails>(routeValues, obj);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, obj.Status);
        Assert.Equal(obj, result.Value);
    }

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
        var result = new AcceptedAtRoute<string>(
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
        var result = new AcceptedAtRoute<object>(routeValues: values, value: null);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
        Assert.Equal(new RouteValueDictionary(values), linkGenerator.RouteValuesAddress.ExplicitValues);
    }

    [Fact]
    public async Task ExecuteResultAsync_ThrowsIfRouteUrlIsNull()
    {
        // Arrange
        var linkGenerator = new TestLinkGenerator();
        var httpContext = GetHttpContext(linkGenerator);

        // Act
        var result = new AcceptedAtRoute<object>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(() =>
            result.ExecuteAsync(httpContext),
            "No route matches the supplied values.");
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        AcceptedAtRoute<Todo> MyApi() { throw new NotImplementedException(); }
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<AcceptedAtRoute<Todo>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status202Accepted, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new AcceptedAtRoute<object>(null, null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<AcceptedAtRoute<object>>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<AcceptedAtRoute<object>>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void AcceptedAtRouteResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Arrange
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new AcceptedAtRoute<string>(routeValues, null));
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
    }

    [Fact]
    public void AcceptedAtRouteResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new AcceptedAtRoute<string>(routeValues, value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRouteResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange & Act
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(new AcceptedAtRoute<string>(routeValues, value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private record Todo(int Id, string Title);

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
