// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Cors;

public class CorsAuthorizationFilterTest
{
    [Theory]
    [InlineData("options")]
    [InlineData("Options")]
    [InlineData("OPTIONS")]
    public async Task CaseInsensitive_PreFlightRequest_SuccessfulMatch_WritesHeaders(string preflightRequestMethod)
    {
        // Arrange
        var mockEngine = GetPassingEngine(supportsCredentials: true);
        var filter = GetFilter(mockEngine);

        var authorizationContext = GetAuthorizationContext(
            new[] { new FilterDescriptor(filter, FilterScope.Action) },
            GetRequestHeaders(true),
            isPreflight: true);
        authorizationContext.HttpContext.Request.Method = preflightRequestMethod;

        // Act
        await filter.OnAuthorizationAsync(authorizationContext);
        await authorizationContext.Result.ExecuteResultAsync(authorizationContext);

        // Assert
        var response = authorizationContext.HttpContext.Response;
        Assert.Equal(204, response.StatusCode);
        Assert.Equal("http://example.com", response.Headers[CorsConstants.AccessControlAllowOrigin]);
        Assert.Equal("header1,header2", response.Headers[CorsConstants.AccessControlAllowHeaders]);

        // Notice: GET header gets filtered because it is a simple header.
        Assert.Equal("PUT", response.Headers[CorsConstants.AccessControlAllowMethods]);
        Assert.Equal("exposed1,exposed2", response.Headers[CorsConstants.AccessControlExposeHeaders]);
        Assert.Equal("123", response.Headers[CorsConstants.AccessControlMaxAge]);
        Assert.Equal("true", response.Headers[CorsConstants.AccessControlAllowCredentials]);
    }

    [Fact]
    public async Task PreFlight_FailedMatch_RespondsWith204NoContent()
    {
        // Arrange
        var mockEngine = GetFailingEngine();
        var filter = GetFilter(mockEngine);

        var authorizationContext = GetAuthorizationContext(
            new[] { new FilterDescriptor(filter, FilterScope.Action) },
            GetRequestHeaders(),
            isPreflight: true);

        // Act
        await filter.OnAuthorizationAsync(authorizationContext);
        await authorizationContext.Result.ExecuteResultAsync(authorizationContext);

        // Assert
        Assert.Equal(204, authorizationContext.HttpContext.Response.StatusCode);
        Assert.Empty(authorizationContext.HttpContext.Response.Headers);
    }

    [Fact]
    public async Task CorsRequest_SuccessfulMatch_WritesHeaders()
    {
        // Arrange
        var mockEngine = GetPassingEngine(supportsCredentials: true);
        var filter = GetFilter(mockEngine);

        var authorizationContext = GetAuthorizationContext(
            new[] { new FilterDescriptor(filter, FilterScope.Action) },
            GetRequestHeaders(true),
            isPreflight: true);

        // Act
        await filter.OnAuthorizationAsync(authorizationContext);
        await authorizationContext.Result.ExecuteResultAsync(authorizationContext);

        // Assert
        var response = authorizationContext.HttpContext.Response;
        Assert.Equal(204, response.StatusCode);
        Assert.Equal("http://example.com", response.Headers[CorsConstants.AccessControlAllowOrigin]);
        Assert.Equal("exposed1,exposed2", response.Headers[CorsConstants.AccessControlExposeHeaders]);
    }

    [Fact]
    public async Task CorsRequest_FailedMatch_Writes200()
    {
        // Arrange
        var mockEngine = GetFailingEngine();
        var filter = GetFilter(mockEngine);

        var authorizationContext = GetAuthorizationContext(
            new[] { new FilterDescriptor(filter, FilterScope.Action) },
            GetRequestHeaders(),
            isPreflight: false);

        // Act
        await filter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Equal(200, authorizationContext.HttpContext.Response.StatusCode);
        Assert.Empty(authorizationContext.HttpContext.Response.Headers);
    }

    private CorsAuthorizationFilter GetFilter(ICorsService corsService)
    {
        var policyProvider = new Mock<ICorsPolicyProvider>();
        policyProvider
            .Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult(new CorsPolicy()));

        return new CorsAuthorizationFilter(corsService, policyProvider.Object, Mock.Of<ILoggerFactory>())
        {
            PolicyName = string.Empty
        };
    }

    private AuthorizationFilterContext GetAuthorizationContext(
        FilterDescriptor[] filterDescriptors,
        RequestHeaders headers = null,
        bool isPreflight = false)
    {
        // HttpContext
        var httpContext = new DefaultHttpContext();
        if (headers != null)
        {
            httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestHeaders, headers.Headers.Split(','));
            httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, new[] { headers.Method });
            httpContext.Request.Headers.Add(CorsConstants.AccessControlExposeHeaders, headers.ExposedHeaders.Split(','));
            httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { headers.Origin });
        }

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        httpContext.RequestServices = services.BuildServiceProvider();

        var method = isPreflight ? CorsConstants.PreflightHttpMethod : "GET";
        httpContext.Request.Method = method;

        // AuthorizationFilterContext
        var actionContext = new ActionContext(
            httpContext: httpContext,
            routeData: new RouteData(),
            actionDescriptor: new ActionDescriptor() { FilterDescriptors = filterDescriptors });

        var authorizationContext = new AuthorizationFilterContext(
            actionContext,
            filterDescriptors.Select(filter => filter.Filter).ToList()
        );

        return authorizationContext;
    }

    private ICorsService GetFailingEngine()
    {
        var mockEngine = new Mock<ICorsService>();
        var result = GetCorsResult("http://example.com");

        mockEngine
            .Setup(o => o.EvaluatePolicy(It.IsAny<HttpContext>(), It.IsAny<CorsPolicy>()))
            .Returns(result);
        return mockEngine.Object;
    }

    private ICorsService GetPassingEngine(bool supportsCredentials = false)
    {
        var mockEngine = new Mock<ICorsService>();
        var result = GetCorsResult(
            "http://example.com",
            new List<string> { "header1", "header2" },
            new List<string> { "PUT" },
            new List<string> { "exposed1", "exposed2" },
            123,
            supportsCredentials);

        mockEngine
            .Setup(o => o.EvaluatePolicy(It.IsAny<HttpContext>(), It.IsAny<CorsPolicy>()))
            .Returns(result);

        mockEngine
            .Setup(o => o.ApplyResult(It.IsAny<CorsResult>(), It.IsAny<HttpResponse>()))
            .Callback<CorsResult, HttpResponse>((result1, response1) =>
            {
                var headers = response1.Headers;
                headers[CorsConstants.AccessControlMaxAge] =
                    result1.PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                headers[CorsConstants.AccessControlAllowOrigin] = result1.AllowedOrigin;
                if (result1.SupportsCredentials)
                {
                    headers.Add(CorsConstants.AccessControlAllowCredentials, new[] { "true" });
                }

                headers.Add(CorsConstants.AccessControlAllowHeaders, result1.AllowedHeaders.ToArray());
                headers.Add(CorsConstants.AccessControlAllowMethods, result1.AllowedMethods.ToArray());
                headers.Add(CorsConstants.AccessControlExposeHeaders, result1.AllowedExposedHeaders.ToArray());
            });

        return mockEngine.Object;
    }

    private RequestHeaders GetRequestHeaders(bool supportsCredentials = false)
    {
        return new RequestHeaders
        {
            Origin = "http://example.com",
            Headers = "header1,header2",
            Method = "GET",
            ExposedHeaders = "exposed1,exposed2",
        };
    }

    private CorsResult GetCorsResult(
        string origin = null,
        IList<string> headers = null,
        IList<string> methods = null,
        IList<string> exposedHeaders = null,
        long? preFlightMaxAge = null,
        bool? supportsCredentials = null)
    {
        var result = new CorsResult();

        if (origin != null)
        {
            result.AllowedOrigin = origin;
        }

        if (headers != null)
        {
            AddRange(result.AllowedHeaders, headers);
        }

        if (methods != null)
        {
            AddRange(result.AllowedMethods, methods);
        }

        if (exposedHeaders != null)
        {
            AddRange(result.AllowedExposedHeaders, exposedHeaders);
        }

        if (preFlightMaxAge != null)
        {
            result.PreflightMaxAge = TimeSpan.FromSeconds(preFlightMaxAge.Value);
        }

        if (supportsCredentials != null)
        {
            result.SupportsCredentials = supportsCredentials.Value;
        }

        return result;
    }

    private void AddRange(IList<string> target, IList<string> source)
    {
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private class RequestHeaders
    {
        public string Origin { get; set; }

        public string Headers { get; set; }

        public string ExposedHeaders { get; set; }

        public string Method { get; set; }
    }
}
