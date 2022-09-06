// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

public class BadRequestOfTResultTests
{
    [Fact]
    public void BadRequestObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var badRequestObjectResult = new BadRequest<object>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
        Assert.Equal(obj, badRequestObjectResult.Value);
    }

    [Fact]
    public void BadRequestObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new HttpValidationProblemDetails();
        var result = new BadRequest<HttpValidationProblemDetails>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public async Task BadRequestObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new BadRequest<string>("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task BadRequestObjectResult_ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var result = new BadRequest<string>("Hello");
        var stream = new MemoryStream();
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
            Response =
                {
                    Body = stream,
                },
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("\"Hello\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task BadRequestObjectResult_ExecuteResultAsync_UsesStatusCodeFromResultTypeForProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, };
        var result = new BadRequest<ProblemDetails>(details);

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices(),
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        BadRequest<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0);

        // Act
        PopulateMetadata<BadRequest<Todo>>(((Delegate)MyApi).GetMethodInfo(), builder);

        // Assert
        var producesResponseTypeMetadata = builder.Metadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status400BadRequest, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new BadRequest<object>(null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("method", () => PopulateMetadata<BadRequest<object>>(null, new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/"), order: 0)));
        Assert.Throws<ArgumentNullException>("builder", () => PopulateMetadata<BadRequest<object>>(((Delegate)PopulateMetadata_ThrowsArgumentNullException_WhenMethodOrBuilderAreNull).GetMethodInfo(), null));
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new BadRequest<string>(null));
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IValueHttpResult_Correctly()
    {
        // Arrange
        var value = "Foo";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult>(new BadRequest<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequestObjectResult_Implements_IValueHttpResultOfT_Correctly()
    {
        // Arrange & Act
        var value = "Foo";

        // Assert
        var result = Assert.IsAssignableFrom<IValueHttpResult<string>>(new BadRequest<string>(value));
        Assert.IsType<string>(result.Value);
        Assert.Equal(value, result.Value);
    }

    private static void PopulateMetadata<TResult>(MethodInfo method, EndpointBuilder builder)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(method, builder);

    private record Todo(int Id, string Title);

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
