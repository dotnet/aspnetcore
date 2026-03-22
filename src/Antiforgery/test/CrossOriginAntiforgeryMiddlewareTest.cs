// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class CrossOriginAntiforgeryMiddlewareTest
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task AllowsRequest_WhenCrossOriginReturnsAllowed(string method)
    {
        var crossOrigin = new Mock<ICrossOriginAntiforgery>();
        crossOrigin.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Allowed);
        var nextCalled = false;
        var middleware = new CrossOriginAntiforgeryMiddleware(crossOrigin.Object, hc =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await middleware.Invoke(httpContext);

        Assert.True(nextCalled);
        Assert.Null(httpContext.Features.Get<IAntiforgeryValidationFeature>());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task DeniesRequest_WhenCrossOriginReturnsDenied(string method)
    {
        var crossOrigin = new Mock<ICrossOriginAntiforgery>();
        crossOrigin.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Denied);
        var middleware = new CrossOriginAntiforgeryMiddleware(crossOrigin.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await middleware.Invoke(httpContext);

        Assert.False(httpContext.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task DeniesRequest_WhenCrossOriginReturnsUnknown(string method)
    {
        var crossOrigin = new Mock<ICrossOriginAntiforgery>();
        crossOrigin.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Unknown);
        var middleware = new CrossOriginAntiforgeryMiddleware(crossOrigin.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await middleware.Invoke(httpContext);

        Assert.False(httpContext.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task SkipsValidation_ForSafeMethods(string method)
    {
        var crossOrigin = new Mock<ICrossOriginAntiforgery>();
        var middleware = new CrossOriginAntiforgeryMiddleware(crossOrigin.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await middleware.Invoke(httpContext);

        crossOrigin.Verify(c => c.Validate(It.IsAny<HttpContext>()), Times.Never());
    }

    [Fact]
    public async Task SkipsValidation_WhenEndpointHasIgnoreMetadata()
    {
        var crossOrigin = new Mock<ICrossOriginAntiforgery>();
        var middleware = new CrossOriginAntiforgeryMiddleware(crossOrigin.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext(hasIgnoreMetadata: true);
        httpContext.Request.Method = "POST";

        await middleware.Invoke(httpContext);

        crossOrigin.Verify(c => c.Validate(It.IsAny<HttpContext>()), Times.Never());
    }

    private static DefaultHttpContext GetHttpContext(bool hasIgnoreMetadata = false)
    {
        var httpContext = new DefaultHttpContext();
        var metadata = !hasIgnoreMetadata
            ? new EndpointMetadataCollection(new AntiforgeryMetadata(!hasIgnoreMetadata))
            : new EndpointMetadataCollection();
        httpContext.SetEndpoint(new Endpoint(null, metadata, "TestEndpoint"));

        return httpContext;
    }
}
