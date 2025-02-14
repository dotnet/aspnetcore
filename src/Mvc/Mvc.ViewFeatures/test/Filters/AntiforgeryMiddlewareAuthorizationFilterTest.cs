// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Core.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Test.Filters;

public class AntiforgeryMiddlewareAuthorizationFilterTest
{
    [Fact]
    public async Task FiltersWorks_MiddlewareInvoked_InvalidFeature()
    {
        // Arrange
        var filter = new AntiforgeryMiddlewareAuthorizationFilter(NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[AntiforgeryMiddlewareAuthorizationFilter.AntiforgeryMiddlewareWithEndpointInvokedKey] = new object();
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, new AntiforgeryValidationException(string.Empty)));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<AntiforgeryValidationFailedResult>(context.Result);
    }

    [Fact]
    public async Task FiltersWorks_MiddlewareInvoked_ValidFeature()
    {
        // Arrange
        var filter = new AntiforgeryMiddlewareAuthorizationFilter(NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[AntiforgeryMiddlewareAuthorizationFilter.AntiforgeryMiddlewareWithEndpointInvokedKey] = new object();
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(true, null));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task FiltersWorks_MiddlewareNotInvoked_InvalidFeature()
    {
        // Arrange
        var filter = new AntiforgeryMiddlewareAuthorizationFilter(NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, new AntiforgeryValidationException(string.Empty)));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task FiltersWorks_MiddlewareNotInvoked_ValidFeature()
    {
        // Arrange
        var filter = new AntiforgeryMiddlewareAuthorizationFilter(NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(true, null));
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new[] { filter });

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result);
    }

    private class AntiforgeryValidationFeature(bool isValid, AntiforgeryValidationException exception) : IAntiforgeryValidationFeature
    {
        public bool IsValid { get; } = isValid;
        public Exception Error { get; } = exception;
    }
}
