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
    public async Task AllowsRequestWhenCrossOriginValidationReturnsAllowed(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var crossOriginAntiforgery = new Mock<ICrossOriginAntiforgery>();
        crossOriginAntiforgery.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Allowed);
        var nextCalled = false;
        var antiforgeryMiddleware = new AntiforgeryMiddleware(crossOriginAntiforgery.Object, antiforgeryService.Object, hc =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        Assert.True(nextCalled);
        // Token validation should NOT be called when cross-origin validation allows the request
        antiforgeryService.Verify(af => af.ValidateRequestAsync(httpContext), Times.Never());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task DeniesRequestWhenCrossOriginValidationReturnsDenied(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var crossOriginAntiforgery = new Mock<ICrossOriginAntiforgery>();
        crossOriginAntiforgery.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Denied);
        var nextCalled = false;
        var antiforgeryMiddleware = new AntiforgeryMiddleware(crossOriginAntiforgery.Object, antiforgeryService.Object, hc =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        Assert.True(nextCalled);
        // Token validation should NOT be called when cross-origin validation denies the request
        antiforgeryService.Verify(af => af.ValidateRequestAsync(httpContext), Times.Never());
        // The validation feature should indicate invalid
        Assert.False(httpContext.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task FallsBackToTokenValidationWhenCrossOriginValidationReturnsUnknown(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var crossOriginAntiforgery = new Mock<ICrossOriginAntiforgery>();
        crossOriginAntiforgery.Setup(c => c.Validate(It.IsAny<HttpContext>())).Returns(CrossOriginValidationResult.Unknown);
        antiforgeryService.Setup(af => af.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var antiforgeryMiddleware = new AntiforgeryMiddleware(crossOriginAntiforgery.Object, antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        // Token validation SHOULD be called when cross-origin validation is unknown
        antiforgeryService.Verify(af => af.ValidateRequestAsync(httpContext), Times.Once());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task DoesNotInvokeCrossOriginValidationForSafeMethods(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var crossOriginAntiforgery = new Mock<ICrossOriginAntiforgery>();
        var antiforgeryMiddleware = new AntiforgeryMiddleware(crossOriginAntiforgery.Object, antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        // Cross-origin validation should NOT be called for safe methods
        crossOriginAntiforgery.Verify(c => c.Validate(It.IsAny<HttpContext>()), Times.Never());
        // Token validation should also NOT be called for safe methods
        antiforgeryService.Verify(af => af.ValidateRequestAsync(httpContext), Times.Never());
    }

    [Fact]
    public async Task DoesNotInvokeCrossOriginValidationWhenEndpointHasIgnoreMetadata()
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var crossOriginAntiforgery = new Mock<ICrossOriginAntiforgery>();
        var antiforgeryMiddleware = new AntiforgeryMiddleware(crossOriginAntiforgery.Object, antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext(hasIgnoreMetadata: true);
        httpContext.Request.Method = "POST";

        await antiforgeryMiddleware.Invoke(httpContext);

        // Cross-origin validation should NOT be called when endpoint has ignore metadata
        crossOriginAntiforgery.Verify(c => c.Validate(It.IsAny<HttpContext>()), Times.Never());
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
