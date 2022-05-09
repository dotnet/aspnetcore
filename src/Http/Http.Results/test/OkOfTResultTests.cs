// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class OkOfTResultTests
{
    [Fact]
    public void OkObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var value = "Hello world";
        var result = new Ok<string>(value);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void OkObjectResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new HttpValidationProblemDetails();
        var result = new Ok<HttpValidationProblemDetails>(obj);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public async Task OkObjectResult_ExecuteAsync_FormatsData()
    {
        // Arrange
        var result = new Ok<string>("Hello");
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
    public async Task OkObjectResult_ExecuteAsync_SetsStatusCode()
    {
        // Arrange
        var result = new Ok<string>("Hello");
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = CreateServices()
        };

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        Ok<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        // Act
        PopulateMetadata<Ok<Todo>>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status200OK, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new Ok<object>(null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<Ok<object>>(null));
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

    private record Todo(int Id, string Title);

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        return services.BuildServiceProvider();
    }
}
