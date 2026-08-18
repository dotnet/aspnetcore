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
    public void DisableTokenGeneration_PreventsTokenMinting_EvenIfAntiforgeryMiddlewareRan()
    {
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var provider = new EndpointAntiforgeryStateProvider(antiforgery.Object);

        var context = new DefaultHttpContext();
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        provider.SetRequestContext(context);
        provider.DisableTokenGeneration();

        var token = provider.GetAntiforgeryToken();

        Assert.Null(token);
        antiforgery.VerifyNoOtherCalls();
    }
}
