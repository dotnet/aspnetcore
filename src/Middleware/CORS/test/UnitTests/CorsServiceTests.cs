// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsServiceTests
{
    [Fact]
    public void EvaluatePolicy_Throws_IfPolicyIsIncorrectlyConfigured()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext("POST", origin: null);
        var policy = new CorsPolicy
        {
            Origins = { "*" },
            SupportsCredentials = true,
        };

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => corsService.EvaluatePolicy(requestContext, policy),
            "policy",
            Resources.InsecureConfiguration);
    }

    [Fact]
    public void EvaluatePolicy_NoOrigin_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext("GET", origin: null);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, new CorsPolicy());

        // Assert
        Assert.Null(result.AllowedOrigin);
        Assert.False(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_NoMatchingOrigin_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add("bar");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.False(result.IsOriginAllowed);
    }

    [Fact]
    public void EvaluatePolicy_EmptyOriginsPolicy_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.False(result.IsOriginAllowed);
    }

    [Fact]
    public void EvaluatePolicy_IsOriginAllowedReturnsFalse_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy()
        {
            IsOriginAllowed = origin => false
        };
        policy.Origins.Add("example.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.False(result.IsOriginAllowed);
    }

    [Fact]
    public void EvaluatePolicy_AllowAnyOrigin_DoesNotSupportCredentials_EmitsOriginHeader()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");

        var policy = new CorsPolicy
        {
            SupportsCredentials = false
        };

        policy.Origins.Add(CorsConstants.AnyOrigin);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("*", result.AllowedOrigin);
    }

    [Fact]
    public void EvaluatePolicy_AllowAnyOrigin_AddsAnyOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("*", result.AllowedOrigin);
    }

    [Fact]
    public void EvaluatePolicy_DoesNotSupportCredentials_AllowCredentialsReturnsFalse()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy
        {
            SupportsCredentials = false
        };
        policy.Origins.Add(CorsConstants.AnyOrigin);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.False(result.SupportsCredentials);
    }

    [Fact]
    public void EvaluatePolicy_SupportsCredentials_AllowCredentialsReturnsTrue()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy
        {
            SupportsCredentials = true
        };
        policy.Origins.Add("http://example.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.True(result.SupportsCredentials);
    }

    [Fact]
    public void EvaluatePolicy_AllowAnyOrigin_DoesNotSupportsCredentials_DoesNotVaryByOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("*", result.AllowedOrigin);
        Assert.False(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_AllowOneOrigin_DoesNotVaryByOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add("http://example.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("http://example.com", result.AllowedOrigin);
        Assert.False(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_AllowMultipleOrigins_VariesByOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add("http://example.com");
        policy.Origins.Add("http://api.example.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("http://example.com", result.AllowedOrigin);
        Assert.True(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_SetIsOriginAllowed_VariesByOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.IsOriginAllowed = origin => true;

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("http://example.com", result.AllowedOrigin);
        Assert.True(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_NoExposedHeaders_NoAllowExposedHeaders()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Empty(result.AllowedExposedHeaders);
    }

    [Fact]
    public void EvaluatePolicy_OneExposedHeaders_HeadersAllowed()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.ExposedHeaders.Add("foo");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "foo" }, result.AllowedExposedHeaders);
    }

    [Fact]
    public void EvaluatePolicy_ManyExposedHeaders_HeadersAllowed()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.ExposedHeaders.Add("foo");
        policy.ExposedHeaders.Add("bar");
        policy.ExposedHeaders.Add("baz");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "foo", "bar", "baz" }, result.AllowedExposedHeaders);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_MethodNotAllowed()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add("GET");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "GET" }, result.AllowedMethods);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_MethodAllowed_ReturnsAllowMethods()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add("PUT");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new[] { "PUT" }, result.AllowedMethods);
    }

    [Theory]
    [InlineData("OpTions")]
    [InlineData("OPTIONS")]
    public void EvaluatePolicy_CaseInsensitivePreflightRequest_OriginAllowed_ReturnsOrigin(string preflightMethod)
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(
            method: preflightMethod,
            origin: "http://example.com",
            accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Origins.Add("http://example.com");
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("http://example.com", result.AllowedOrigin);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_IsOriginAllowedReturnsTrue_ReturnsOrigin()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(
            method: "OPTIONS",
            origin: "http://example.com",
            accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy
        {
            IsOriginAllowed = origin => true
        };
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal("http://example.com", result.AllowedOrigin);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_SupportsCredentials_AllowCredentialsReturnsTrue()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy
        {
            SupportsCredentials = true
        };
        policy.Origins.Add("http://example.com");
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.True(result.SupportsCredentials);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_NoPreflightMaxAge_NoPreflightMaxAgeSet()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy
        {
            PreflightMaxAge = null
        };
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Null(result.PreflightMaxAge);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_PreflightMaxAge_PreflightMaxAgeSet()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy
        {
            PreflightMaxAge = TimeSpan.FromSeconds(10)
        };
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), result.PreflightMaxAge);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_AnyMethod_ReturnsRequestMethod()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "GET");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "GET" }, result.AllowedMethods);
    }

    [Theory]
    [InlineData("Put")]
    [InlineData("PUT")]
    public void EvaluatePolicy_CaseInsensitivePreflightRequest_ReturnsAllowedMethods(string method)
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(
            method: "OPTIONS",
            origin: "http://example.com",
            accessControlRequestMethod: method);
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add("PUT");
        policy.Methods.Add("DELETE");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "PUT", "DELETE" }, result.AllowedMethods);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_NoHeadersRequested_AllowedAllHeaders()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);
        policy.Headers.Add(CorsConstants.AnyHeader);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Empty(result.AllowedHeaders);
        Assert.Equal(new[] { "PUT" }, result.AllowedMethods);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_AllowAllHeaders_ReflectsRequestHeaders()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(
            method: "OPTIONS",
            origin: "http://example.com",
            accessControlRequestMethod: "PUT",
            accessControlRequestHeaders: new[] { "foo", "bar" });
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);
        policy.Headers.Add(CorsConstants.AnyHeader);

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "foo", "bar" }, result.AllowedHeaders);
        Assert.Equal(new[] { "PUT" }, result.AllowedMethods);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_HeadersRequested_NotAllHeaderMatches_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(
            method: "OPTIONS",
            origin: "http://example.com",
            accessControlRequestMethod: "PUT",
            accessControlRequestHeaders: new[] { "match", "noMatch" });
        var policy = new CorsPolicy();
        policy.Origins.Add(CorsConstants.AnyOrigin);
        policy.Methods.Add(CorsConstants.AnyMethod);
        policy.Headers.Add("match");
        policy.Headers.Add("foo");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.Equal(new[] { "match", "foo" }, result.AllowedHeaders);
        Assert.Equal(new[] { "PUT" }, result.AllowedMethods);
    }

    [Fact]
    public void EvaluatePolicy_PreflightRequest_WithCredentials_ReflectsHeaders()
    {
        // Arrange
        var corsService = GetCorsService();
        var httpContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
        var policy = new CorsPolicy();
        policy.Origins.Add("http://example.com");
        policy.Methods.Add(CorsConstants.AnyMethod);
        policy.Headers.Add(CorsConstants.AnyHeader);
        policy.SupportsCredentials = true;

        // Act
        var result = corsService.EvaluatePolicy(httpContext, policy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new[] { "PUT" }, result.AllowedMethods);
        Assert.Empty(result.AllowedHeaders);
        Assert.True(result.SupportsCredentials);
    }

    [Fact]
    public void ApplyResult_ReturnsNoHeaders_ByDefault()
    {
        // Arrange
        var result = new CorsResult();
        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Empty(httpContext.Response.Headers);
    }

    [Fact]
    public void ApplyResult_AllowOrigin_AllowOriginHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            AllowedOrigin = "http://example.com"
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("http://example.com", httpContext.Response.Headers["Access-Control-Allow-Origin"]);
    }

    [Fact]
    public void ApplyResult_NoAllowOrigin_AllowOriginHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            AllowedOrigin = null
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Allow-Origin", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_AllowCredentials_AllowCredentialsHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            SupportsCredentials = true
        };

        var service = GetCorsService();

        // Act
        var httpContext = new DefaultHttpContext();
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("true", httpContext.Response.Headers["Access-Control-Allow-Credentials"]);
    }

    [Fact]
    public void ApplyResult_AddVaryHeader_VaryHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            VaryByOrigin = true
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("Origin", httpContext.Response.Headers["Vary"]);
    }

    [Fact]
    public void ApplyResult_AppendsVaryHeader()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            VaryByOrigin = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Headers["Vary"] = "Cookie";
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("Cookie,Origin", httpContext.Response.Headers["Vary"]);
    }

    [Fact]
    public void ApplyResult_NoAllowCredentials_AllowCredentialsHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            SupportsCredentials = false
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Allow-Credentials", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_NoAllowMethods_AllowMethodsHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            // AllowMethods is empty by default
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Allow-Methods", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_OneAllowMethods_AllowMethodsHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = true,
            AllowedMethods = { "PUT" }
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("PUT", httpContext.Response.Headers["Access-Control-Allow-Methods"]);
    }

    [Fact]
    public void ApplyResult_NoAllowHeaders_AllowHeadersHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            // AllowHeaders is empty by default
            IsOriginAllowed = true,
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Allow-Headers", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_OneAllowHeaders_AllowHeadersHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = true,
            AllowedHeaders = { "foo" }
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("foo", httpContext.Response.Headers["Access-Control-Allow-Headers"]);
    }

    [Fact]
    public void ApplyResult_NoAllowExposedHeaders_ExposedHeadersHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            // AllowExposedHeaders is empty by default
            IsOriginAllowed = true,
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Expose-Headers", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_PreflightRequest_ExposesHeadersNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = true,
            AllowedExposedHeaders = { "foo", "bar" },
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Expose-Headers", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_NoPreflightRequest_ExposesHeadersAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = false,
            AllowedExposedHeaders = { "foo", "bar" },
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("foo,bar", httpContext.Response.Headers[CorsConstants.AccessControlExposeHeaders]);
    }

    [Fact]
    public void ApplyResult_OneAllowExposedHeaders_ExposedHeadersHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            AllowedExposedHeaders = { "foo" },
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("foo", httpContext.Response.Headers["Access-Control-Expose-Headers"]);
    }

    [Fact]
    public void ApplyResult_NoPreflightMaxAge_MaxAgeHeaderNotAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = false,
            PreflightMaxAge = TimeSpan.FromSeconds(30),
        };

        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.DoesNotContain("Access-Control-Max-Age", httpContext.Response.Headers.Keys);
    }

    [Fact]
    public void ApplyResult_PreflightMaxAge_MaxAgeHeaderAdded()
    {
        // Arrange
        var result = new CorsResult
        {
            IsOriginAllowed = true,
            IsPreflightRequest = true,
            PreflightMaxAge = TimeSpan.FromSeconds(30),
        };
        var httpContext = new DefaultHttpContext();
        var service = GetCorsService();

        // Act
        service.ApplyResult(result, httpContext.Response);

        // Assert
        Assert.Equal("30", httpContext.Response.Headers["Access-Control-Max-Age"]);
    }

    [Fact]
    public void EvaluatePolicy_MultiOriginsPolicy_ReturnsVaryByOriginHeader()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add("http://example.com");
        policy.Origins.Add("http://example-two.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.NotNull(result.AllowedOrigin);
        Assert.True(result.VaryByOrigin);
    }

    [Fact]
    public void EvaluatePolicy_MultiOriginsPolicy_NoMatchingOrigin_ReturnsInvalidResult()
    {
        // Arrange
        var corsService = GetCorsService();
        var requestContext = GetHttpContext(origin: "http://example.com");
        var policy = new CorsPolicy();
        policy.Origins.Add("http://example-two.com");
        policy.Origins.Add("http://example-three.com");

        // Act
        var result = corsService.EvaluatePolicy(requestContext, policy);

        // Assert
        Assert.False(result.IsOriginAllowed);
    }

    private static CorsService GetCorsService(CorsOptions options = null)
    {
        options = options ?? new CorsOptions();
        return new CorsService(Options.Create(options), NullLoggerFactory.Instance);
    }

    private static HttpContext GetHttpContext(
        string method = null,
        string origin = null,
        string accessControlRequestMethod = null,
        string[] accessControlRequestHeaders = null)
    {
        var context = new DefaultHttpContext();

        if (method != null)
        {
            context.Request.Method = method;
        }

        if (origin != null)
        {
            context.Request.Headers.Add(CorsConstants.Origin, new[] { origin });
        }

        if (accessControlRequestMethod != null)
        {
            context.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, new[] { accessControlRequestMethod });
        }

        if (accessControlRequestHeaders != null)
        {
            context.Request.Headers.Add(CorsConstants.AccessControlRequestHeaders, accessControlRequestHeaders);
        }

        return context;
    }
}
