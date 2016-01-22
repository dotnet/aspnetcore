// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Cors.Infrastructure
{
    public class CorsServiceTests
    {
        [Fact]
        public void EvaluatePolicy_NoOrigin_ReturnsInvalidResult()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
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
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy();
            policy.Origins.Add("bar");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Null(result.AllowedOrigin);
            Assert.False(result.VaryByOrigin);
        }

        [Fact]
        public void EvaluatePolicy_EmptyOriginsPolicy_ReturnsInvalidResult()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy();

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Null(result.AllowedOrigin);
            Assert.False(result.VaryByOrigin);
        }

        [Fact]
        public void EvaluatePolicy_AllowAnyOrigin_DoesNotSupportCredentials_EmitsWildcardForOrigin()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
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
        public void EvaluatePolicy_AllowAnyOrigin_SupportsCredentials_AddsSpecificOrigin()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy
            {
                SupportsCredentials = true
            };
            policy.Origins.Add(CorsConstants.AnyOrigin);

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal("http://example.com", result.AllowedOrigin);
            Assert.True(result.VaryByOrigin);
        }

        [Fact]
        public void EvaluatePolicy_DoesNotSupportCredentials_AllowCredentialsReturnsFalse()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
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
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy
            {
                SupportsCredentials = true
            };
            policy.Origins.Add(CorsConstants.AnyOrigin);

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.True(result.SupportsCredentials);
        }

        [Fact]
        public void EvaluatePolicy_NoExposedHeaders_NoAllowExposedHeaders()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
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
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.ExposedHeaders.Add("foo");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(1, result.AllowedExposedHeaders.Count);
            Assert.Contains("foo", result.AllowedExposedHeaders);
        }

        [Fact]
        public void EvaluatePolicy_ManyExposedHeaders_HeadersAllowed()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(origin: "http://example.com");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.ExposedHeaders.Add("foo");
            policy.ExposedHeaders.Add("bar");
            policy.ExposedHeaders.Add("baz");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(3, result.AllowedExposedHeaders.Count);
            Assert.Contains("foo", result.AllowedExposedHeaders);
            Assert.Contains("bar", result.AllowedExposedHeaders);
            Assert.Contains("baz", result.AllowedExposedHeaders);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_MethodNotAllowed_ReturnsInvalidResult()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("GET");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Empty(result.AllowedMethods);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_MethodAllowed_ReturnsAllowMethods()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("PUT");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("PUT", result.AllowedMethods);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_OriginAllowed_ReturnsOrigin()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Origins.Add("http://example.com");
            policy.Methods.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal("http://example.com", result.AllowedOrigin);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_SupportsCredentials_AllowCredentialsReturnsTrue()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy
            {
                SupportsCredentials = true
            };
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.True(result.SupportsCredentials);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_NoPreflightMaxAge_NoPreflightMaxAgeSet()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy
            {
                PreflightMaxAge = null
            };
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Null(result.PreflightMaxAge);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_PreflightMaxAge_PreflightMaxAgeSet()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy
            {
                PreflightMaxAge = TimeSpan.FromSeconds(10)
            };
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(10), result.PreflightMaxAge);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_AnyMethod_ReturnsRequestMethod()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "GET");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(1, result.AllowedMethods.Count);
            Assert.Contains("GET", result.AllowedMethods);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_ListedMethod_ReturnsSubsetOfListedMethods()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("PUT");
            policy.Methods.Add("DELETE");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(1, result.AllowedMethods.Count);
            Assert.Contains("PUT", result.AllowedMethods);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_NoHeadersRequested_AllowedAllHeaders_ReturnsEmptyHeaders()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(method: "OPTIONS", origin: "http://example.com", accessControlRequestMethod: "PUT");
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");
            policy.Headers.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Empty(result.AllowedHeaders);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_HeadersRequested_AllowAllHeaders_ReturnsRequestedHeaders()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(
                method: "OPTIONS",
                origin: "http://example.com",
                accessControlRequestMethod: "PUT",
                accessControlRequestHeaders: new[] { "foo", "bar" });
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");
            policy.Headers.Add("*");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(2, result.AllowedHeaders.Count);
            Assert.Contains("foo", result.AllowedHeaders);
            Assert.Contains("bar", result.AllowedHeaders);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_HeadersRequested_AllowSomeHeaders_ReturnsSubsetOfListedHeaders()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(
                method: "OPTIONS",
                origin: "http://example.com",
                accessControlRequestMethod: "PUT",
                accessControlRequestHeaders: new[] { "content-type", "accept" });
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");
            policy.Headers.Add("foo");
            policy.Headers.Add("bar");
            policy.Headers.Add("Content-Type");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Equal(2, result.AllowedHeaders.Count);
            Assert.Contains("Content-Type", result.AllowedHeaders, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void EvaluatePolicy_PreflightRequest_HeadersRequested_NotAllHeaderMatches_ReturnsInvalidResult()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());
            var requestContext = GetHttpContext(
                method: "OPTIONS",
                origin: "http://example.com",
                accessControlRequestMethod: "PUT",
                accessControlRequestHeaders: new[] { "match", "noMatch" });
            var policy = new CorsPolicy();
            policy.Origins.Add(CorsConstants.AnyOrigin);
            policy.Methods.Add("*");
            policy.Headers.Add("match");
            policy.Headers.Add("foo");

            // Act
            var result = corsService.EvaluatePolicy(requestContext, policy);

            // Assert
            Assert.Empty(result.AllowedHeaders);
            Assert.Empty(result.AllowedMethods);
            Assert.Empty(result.AllowedExposedHeaders);
            Assert.Null(result.AllowedOrigin);
        }

        [Fact]
        public void EaluatePolicy_DoesCaseSensitiveComparison()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());

            var policy = new CorsPolicy();
            policy.Methods.Add("POST");
            var httpContext = GetHttpContext(origin: null, accessControlRequestMethod: "post");

            // Act
            var result = corsService.EvaluatePolicy(httpContext, policy);

            // Assert
            Assert.Empty(result.AllowedHeaders);
            Assert.Empty(result.AllowedMethods);
            Assert.Empty(result.AllowedExposedHeaders);
            Assert.Null(result.AllowedOrigin);
        }

        [Fact]
        public void TryValidateOrigin_DoesCaseSensitiveComparison()
        {
            // Arrange
            var corsService = new CorsService(new TestCorsOptions());

            var policy = new CorsPolicy();
            policy.Origins.Add("http://Example.com");
            var httpContext = GetHttpContext(origin: "http://example.com");

            // Act
            var result = corsService.EvaluatePolicy(httpContext, policy);

            // Assert
            Assert.Empty(result.AllowedHeaders);
            Assert.Empty(result.AllowedMethods);
            Assert.Empty(result.AllowedExposedHeaders);
            Assert.Null(result.AllowedOrigin);
        }


        [Fact]
        public void ApplyResult_ReturnsNoHeaders_ByDefault()
        {
            // Arrange
            var result = new CorsResult();
            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

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
                AllowedOrigin = "http://example.com"
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

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
                AllowedOrigin = null
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

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
                SupportsCredentials = true
            };

            var service = new CorsService(new TestCorsOptions());

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
                VaryByOrigin = true
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Equal("Origin", httpContext.Response.Headers["Vary"]);
        }

        [Fact]
        public void ApplyResult_NoAllowCredentials_AllowCredentialsHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult
            {
                SupportsCredentials = false
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

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
                // AllowMethods is empty by default
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.DoesNotContain("Access-Control-Allow-Methods", httpContext.Response.Headers.Keys);
        }

        [Fact]
        public void ApplyResult_OneAllowMethods_AllowMethodsHeaderAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedMethods.Add("PUT");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Equal("PUT", httpContext.Response.Headers["Access-Control-Allow-Methods"]);
        }

        [Fact]
        public void ApplyResult_SomeSimpleAllowMethods_AllowMethodsHeaderAddedForNonSimpleMethods()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedMethods.Add("PUT");
            result.AllowedMethods.Add("get");
            result.AllowedMethods.Add("DELETE");
            result.AllowedMethods.Add("POST");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Contains("Access-Control-Allow-Methods", httpContext.Response.Headers.Keys);
            var value = Assert.Single(httpContext.Response.Headers.Values);
            Assert.Equal(new[] { "PUT,DELETE" }, value);
            string[] methods = httpContext.Response.Headers.GetCommaSeparatedValues("Access-Control-Allow-Methods");
            Assert.Equal(2, methods.Length);
            Assert.Contains("PUT", methods);
            Assert.Contains("DELETE", methods);
        }

        [Fact]
        public void ApplyResult_SimpleAllowMethods_AllowMethodsHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedMethods.Add("GET");
            result.AllowedMethods.Add("HEAD");
            result.AllowedMethods.Add("POST");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.DoesNotContain("Access-Control-Allow-Methods", httpContext.Response.Headers.Keys);
        }

        [Fact]
        public void ApplyResult_NoAllowHeaders_AllowHeadersHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult
            {
                // AllowHeaders is empty by default
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.DoesNotContain("Access-Control-Allow-Headers", httpContext.Response.Headers.Keys);
        }

        [Fact]
        public void ApplyResult_OneAllowHeaders_AllowHeadersHeaderAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedHeaders.Add("foo");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Equal("foo", httpContext.Response.Headers["Access-Control-Allow-Headers"]);
        }

        [Fact]
        public void ApplyResult_ManyAllowHeaders_AllowHeadersHeaderAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedHeaders.Add("foo");
            result.AllowedHeaders.Add("bar");
            result.AllowedHeaders.Add("baz");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Contains("Access-Control-Allow-Headers", httpContext.Response.Headers.Keys);
            var value = Assert.Single(httpContext.Response.Headers.Values);
            Assert.Equal(new[] { "foo,bar,baz" }, value);
            string[] headerValues = httpContext.Response.Headers.GetCommaSeparatedValues("Access-Control-Allow-Headers");
            Assert.Equal(3, headerValues.Length);
            Assert.Contains("foo", headerValues);
            Assert.Contains("bar", headerValues);
            Assert.Contains("baz", headerValues);
        }

        [Fact]
        public void ApplyResult_SomeSimpleAllowHeaders_AllowHeadersHeaderAddedForNonSimpleHeaders()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedHeaders.Add("Content-Language");
            result.AllowedHeaders.Add("foo");
            result.AllowedHeaders.Add("bar");
            result.AllowedHeaders.Add("Accept");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Contains("Access-Control-Allow-Headers", httpContext.Response.Headers.Keys);
            string[] headerValues = httpContext.Response.Headers.GetCommaSeparatedValues("Access-Control-Allow-Headers");
            Assert.Equal(2, headerValues.Length);
            Assert.Contains("foo", headerValues);
            Assert.Contains("bar", headerValues);
        }

        [Fact]
        public void ApplyResult_SimpleAllowHeaders_AllowHeadersHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedHeaders.Add("Accept");
            result.AllowedHeaders.Add("Accept-Language");
            result.AllowedHeaders.Add("Content-Language");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.DoesNotContain("Access-Control-Allow-Headers", httpContext.Response.Headers.Keys);
        }

        [Fact]
        public void ApplyResult_NoAllowExposedHeaders_ExposedHeadersHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult
            {
                // AllowExposedHeaders is empty by default
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.DoesNotContain("Access-Control-Expose-Headers", httpContext.Response.Headers.Keys);
        }

        [Fact]
        public void ApplyResult_OneAllowExposedHeaders_ExposedHeadersHeaderAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedExposedHeaders.Add("foo");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Equal("foo", httpContext.Response.Headers["Access-Control-Expose-Headers"]);
        }

        [Fact]
        public void ApplyResult_ManyAllowExposedHeaders_ExposedHeadersHeaderAdded()
        {
            // Arrange
            var result = new CorsResult();
            result.AllowedExposedHeaders.Add("foo");
            result.AllowedExposedHeaders.Add("bar");
            result.AllowedExposedHeaders.Add("baz");

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Contains("Access-Control-Expose-Headers", httpContext.Response.Headers.Keys);
            var value = Assert.Single(httpContext.Response.Headers.Values);
            Assert.Equal(new[] { "foo,bar,baz" }, value);
            string[] exposedHeaderValues = httpContext.Response.Headers.GetCommaSeparatedValues("Access-Control-Expose-Headers");
            Assert.Equal(3, exposedHeaderValues.Length);
            Assert.Contains("foo", exposedHeaderValues);
            Assert.Contains("bar", exposedHeaderValues);
            Assert.Contains("baz", exposedHeaderValues);
        }

        [Fact]
        public void ApplyResult_NoPreflightMaxAge_MaxAgeHeaderNotAdded()
        {
            // Arrange
            var result = new CorsResult
            {
                PreflightMaxAge = null
            };

            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

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
                PreflightMaxAge = TimeSpan.FromSeconds(30)
            };
            var httpContext = new DefaultHttpContext();
            var service = new CorsService(new TestCorsOptions());

            // Act
            service.ApplyResult(result, httpContext.Response);

            // Assert
            Assert.Equal("30", httpContext.Response.Headers["Access-Control-Max-Age"]);
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
}