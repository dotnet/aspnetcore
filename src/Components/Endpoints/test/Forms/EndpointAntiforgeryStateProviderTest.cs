// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.Forms;

public class EndpointAntiforgeryStateProviderTest
{
    [Fact]
    public void GetAntiforgeryToken_ReturnsNull_WhenAntiforgeryMiddlewareDidNotRun()
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);

        var context = new DefaultHttpContext();
        provider.SetRequestContext(context);

        var token = provider.GetAntiforgeryToken();

        Assert.Null(token);
        antiforgery.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetAntiforgeryToken_MintsToken_WhenAntiforgeryMiddlewareRan()
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery
            .Setup(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "__RequestVerificationToken", null));

        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);

        var context = new DefaultHttpContext();
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        provider.SetRequestContext(context);

        var token = provider.GetAntiforgeryToken();

        Assert.NotNull(token);
        Assert.Equal("request-token", token!.Value);
        Assert.Equal("__RequestVerificationToken", token.FormFieldName);
        antiforgery.Verify(a => a.GetAndStoreTokens(context), Times.Once);
    }

    [Fact]
    public void PersistingState_DoesNotDemandTokens()
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);

        var context = new DefaultHttpContext();
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        provider.SetRequestContext(context);

        var token = provider.CurrentToken;

        Assert.Null(token);
        antiforgery.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AfterResponseStarts_ReturnsOnlyPreviouslyDemandedToken(bool demandedBeforeStart)
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery.Setup(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "field", null));
        antiforgery.Setup(a => a.GetTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "field", null));
        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);
        var context = new DefaultHttpContext();
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        provider.SetRequestContext(context);
        var token = demandedBeforeStart ? provider.GetAntiforgeryToken() : null;
        var response = new Mock<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>();
        response.SetupGet(feature => feature.HasStarted).Returns(true);
        context.Features.Set(response.Object);

        Assert.Same(token, provider.GetAntiforgeryToken());
        antiforgery.Verify(a => a.GetAndStoreTokens(context), demandedBeforeStart ? Times.Once() : Times.Never());
        antiforgery.Verify(a => a.GetTokens(context), demandedBeforeStart ? Times.Never() : Times.Once());
        antiforgery.VerifyNoOtherCalls();
    }

    [Fact]
    public void AfterResponseStarts_CanReuseTheExistingCookie()
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery.Setup(a => a.GetTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", null, "field", null));
        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);
        var context = new DefaultHttpContext();
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        var response = new Mock<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>();
        response.SetupGet(feature => feature.HasStarted).Returns(true);
        context.Features.Set(response.Object);
        provider.SetRequestContext(context);

        var token = provider.GetAntiforgeryToken();

        Assert.Equal("request-token", token.Value);
        Assert.Same(token, provider.GetAntiforgeryToken());
        antiforgery.Verify(a => a.GetTokens(context), Times.Once);
        antiforgery.VerifyNoOtherCalls();
    }
}
