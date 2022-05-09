// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class AcceptedOfTResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_FormatsData()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;
        // Act
        var result = new Accepted<string>("my-location", value: "Hello world");
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
        var result = new Accepted<string>(expectedUrl, value: "some-value");
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public void AcceptedResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var expectedUrl = "testAction";
        var obj = new HttpValidationProblemDetails();
        var result = new Accepted<HttpValidationProblemDetails>(expectedUrl, obj);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, obj.Status);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        Accepted<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, null);

        // Act
        PopulateMetadata<Accepted<Todo>>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status202Accepted, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new Accepted<object>("location", null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<Accepted<object>>(null));
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

    private record Todo(int Id, string Title);

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
