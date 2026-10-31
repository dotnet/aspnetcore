// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Cors;

public class DisableCorsAuthorizationFilterTest
{
    [Fact]
    public async Task DisableCors_DoesNotShortCircuitsRequest_IfNotAPreflightRequest()
    {
        // Arrange
        var filter = new DisableCorsAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Headers.Add(CorsConstants.Origin, "http://localhost:5000/");
        httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, "PUT");
        var authorizationFilterContext = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        // Act
        await filter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Null(authorizationFilterContext.Result);
    }

    [Fact]
    public async Task DisableCors_DoesNotShortCircuitsRequest_IfNoAccessControlRequestMethodFound()
    {
        // Arrange
        var filter = new DisableCorsAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "OPTIONS";
        httpContext.Request.Headers.Add(CorsConstants.Origin, "http://localhost:5000/");
        var authorizationFilterContext = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        // Act
        await filter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Null(authorizationFilterContext.Result);
    }

    [Theory]
    [InlineData("OpTions")]
    [InlineData("OPTIONS")]
    public async Task DisableCors_CaseInsensitivePreflightMethod_ShortCircuitsRequest(string preflightMethod)
    {
        // Arrange
        var filter = new DisableCorsAuthorizationFilter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = preflightMethod;
        httpContext.Request.Headers.Add(CorsConstants.Origin, "http://localhost:5000/");
        httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, "PUT");
        var authorizationFilterContext = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        // Act
        await filter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(authorizationFilterContext.Result);
        Assert.Equal(StatusCodes.Status204NoContent, statusCodeResult.StatusCode);
    }
}
