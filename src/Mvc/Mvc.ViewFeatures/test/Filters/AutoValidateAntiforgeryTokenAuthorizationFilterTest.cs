// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class AutoValidateAntiforgeryTokenAuthorizationFilterTest
{
    [Theory]
    [InlineData("PUT")]
    [InlineData("POsT")]
    [InlineData("DeLETE")]
    public async Task Filter_ValidatesAntiforgery_ForUnsafeMethod(string httpMethod)
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.FromResult(0))
            .Verifiable();

        var filter = new AutoValidateAntiforgeryTokenAuthorizationFilter(antiforgery.Object, NullLoggerFactory.Instance);

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        actionContext.HttpContext.Request.Method = httpMethod;

        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        antiforgery.Verify();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("TracE")]
    [InlineData("OPTIONs")]
    public async Task Filter_SkipsAntiforgeryVerification_ForSafeMethod(string httpMethod)
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.FromResult(0))
            .Verifiable();

        var filter = new AutoValidateAntiforgeryTokenAuthorizationFilter(antiforgery.Object, NullLoggerFactory.Instance);

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        actionContext.HttpContext.Request.Method = httpMethod;

        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        antiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never());
    }

    [Fact]
    public async Task Filter_SkipsAntiforgeryVerification_WhenOverridden()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.FromResult(0))
            .Verifiable();

        var filter = new AutoValidateAntiforgeryTokenAuthorizationFilter(antiforgery.Object, NullLoggerFactory.Instance);

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        actionContext.HttpContext.Request.Method = "POST";

        var context = new AuthorizationFilterContext(actionContext, new IFilterMetadata[]
        {
                filter,
                new IgnoreAntiforgeryTokenAttribute(),
        });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        antiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never());
    }
}
