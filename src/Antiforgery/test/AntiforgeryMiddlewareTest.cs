// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class AntiforgeryMiddlewareTest
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task ValidatesAntiforgeryTokenForValidMethods(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        antiforgeryService.Setup(af => af.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.FromResult(true));
        var antiforgeryMiddleware = new AntiforgeryMiddleware(antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        antiforgeryService.Verify(antiforgeryService => antiforgeryService.ValidateRequestAsync(httpContext), Times.AtMostOnce());
        Assert.True(httpContext.Features.Get<IAntiforgeryValidationFeature>()?.IsValid);
    }

    [Fact]
    public async Task RespectsIgnoreAntiforgeryMetadata()
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var antiforgeryMiddleware = new AntiforgeryMiddleware(antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext(hasIgnoreMetadata: true);

        await antiforgeryMiddleware.Invoke(httpContext);

        antiforgeryService.Verify(antiforgeryService => antiforgeryService.ValidateRequestAsync(httpContext), Times.Never());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("TRACE")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("DELETE")]
    [InlineData("CONNECT")]
    public async Task IgnoresUnsupportedHttpMethods(string method)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        var antiforgeryMiddleware = new AntiforgeryMiddleware(antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext();
        httpContext.Request.Method = method;

        await antiforgeryMiddleware.Invoke(httpContext);

        antiforgeryService.Verify(antiforgeryService => antiforgeryService.ValidateRequestAsync(httpContext), Times.Never());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetMiddlewareInvokedProperty(bool hasIgnoreMetadata)
    {
        var antiforgeryService = new Mock<IAntiforgery>();
        antiforgeryService.Setup(af => af.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.FromResult(true));
        var antiforgeryMiddleware = new AntiforgeryMiddleware(antiforgeryService.Object, hc => Task.CompletedTask);
        var httpContext = GetHttpContext(hasIgnoreMetadata);

        await antiforgeryMiddleware.Invoke(httpContext);

        Assert.True(httpContext.Items.ContainsKey("__AntiforgeryMiddlewareWithEndpointInvoked"));
    }

    internal static DefaultHttpContext GetHttpContext(bool hasIgnoreMetadata = false)
    {
        var httpContext = new DefaultHttpContext();
        var metadata = !hasIgnoreMetadata
            ? new EndpointMetadataCollection(new AntiforgeryMetadata(!hasIgnoreMetadata))
            : new EndpointMetadataCollection();
        httpContext.SetEndpoint(new Endpoint(null, metadata, "TestEndpoint"));

        return httpContext;
    }
}
