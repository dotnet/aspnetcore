// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RequireHttpsAttributeTests
    {
        [Fact]
        public void OnAuthorization_AllowsTheRequestIfItIsHttps()
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "https";

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.Null(authContext.Result);
        }

        public static TheoryData<string, string, string, string, string> RedirectToHttpEndpointTestData
        {
            get
            {
                // host, pathbase, path, query, expectedRedirectUrl
                return new TheoryData<string, string, string, string, string>
                {
                    { "localhost", null, null, null, "https://localhost" },
                    { "localhost:5000", null, null, null, "https://localhost" },
                    { "localhost", "/pathbase", null, null, "https://localhost/pathbase" },
                    { "localhost", "/pathbase", "/path", null, "https://localhost/pathbase/path" },
                    { "localhost", "/pathbase", "/path", "?foo=bar", "https://localhost/pathbase/path?foo=bar" },

                    // Encode some special characters on the URL.
                    { "localhost", "/path?base", null, null, "https://localhost/path%3Fbase" },
                    { "localhost", null, "/pa?th", null, "https://localhost/pa%3Fth" },

                    { "localhost", "/", null, "?foo=bar%2Fbaz", "https://localhost/?foo=bar%2Fbaz" },

                    // URLs with punycode
                    // 本地主機 is "localhost" in chinese traditional, "xn--tiq21tzznx7c" is the
                    // punycode representation.
                    { "本地主機", "/", null, null, "https://xn--tiq21tzznx7c/" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RedirectToHttpEndpointTestData))]
        public void OnAuthorization_RedirectsToHttpsEndpoint_ForNonHttpsGetRequests(
            string host,
            string pathBase,
            string path,
            string queryString,
            string expectedUrl)
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.RequestServices = CreateServices();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = "GET";
            requestContext.Request.Host = HostString.FromUriComponent(host);

            if (pathBase != null)
            {
                requestContext.Request.PathBase = new PathString(pathBase);
            }

            if (path != null)
            {
                requestContext.Request.Path = new PathString(path);
            }

            if (queryString != null)
            {
                requestContext.Request.QueryString = new QueryString(queryString);
            }

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.NotNull(authContext.Result);
            var result = Assert.IsType<RedirectResult>(authContext.Result);

            Assert.False(result.Permanent);
            Assert.Equal(expectedUrl, result.Url);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public void OnAuthorization_SignalsBadRequestStatusCode_ForNonHttpsAndNonGetRequests(string method)
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.RequestServices = CreateServices();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = method;
            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.NotNull(authContext.Result);
            var result = Assert.IsType<StatusCodeResult>(authContext.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public void HandleNonHttpsRequestExtensibility()
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.RequestServices = CreateServices();
            requestContext.Request.Scheme = "http";

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new CustomRequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            var result = Assert.IsType<StatusCodeResult>(authContext.Result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Theory]
        [InlineData("http://localhost", null, "https://localhost/")]
        [InlineData("http://localhost:5000", null, "https://localhost/")]
        [InlineData("http://[2001:db8:a0b:12f0::1]", null, "https://[2001:db8:a0b:12f0::1]/")]
        [InlineData("http://[2001:db8:a0b:12f0::1]:5000", null, "https://[2001:db8:a0b:12f0::1]/")]
        [InlineData("http://localhost:5000/path", null, "https://localhost/path")]
        [InlineData("http://localhost:5000/path?foo=bar", null, "https://localhost/path?foo=bar")]
        [InlineData("http://本地主機:5000", null, "https://xn--tiq21tzznx7c/")]
        [InlineData("http://localhost", 44380, "https://localhost:44380/")]
        [InlineData("http://localhost:5000", 44380, "https://localhost:44380/")]
        [InlineData("http://[2001:db8:a0b:12f0::1]", 44380, "https://[2001:db8:a0b:12f0::1]:44380/")]
        [InlineData("http://[2001:db8:a0b:12f0::1]:5000", 44380, "https://[2001:db8:a0b:12f0::1]:44380/")]
        [InlineData("http://localhost:5000/path", 44380, "https://localhost:44380/path")]
        [InlineData("http://localhost:5000/path?foo=bar", 44380, "https://localhost:44380/path?foo=bar")]
        [InlineData("http://本地主機:5000", 44380, "https://xn--tiq21tzznx7c:44380/")]
        public void OnAuthorization_RedirectsToHttpsEndpoint_ForCustomSslPort(
            string url,
            int? sslPort,
            string expectedUrl)
        {
            // Arrange
            var options = Options.Create(new MvcOptions());
            var uri = new Uri(url);

            var requestContext = new DefaultHttpContext();
            requestContext.RequestServices = CreateServices(sslPort);
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = "GET";
            requestContext.Request.Host = HostString.FromUriComponent(uri);
            requestContext.Request.Path = PathString.FromUriComponent(uri);
            requestContext.Request.QueryString = QueryString.FromUriComponent(uri);

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.NotNull(authContext.Result);
            var result = Assert.IsType<RedirectResult>(authContext.Result);

            Assert.Equal(expectedUrl, result.Url);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(null, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void OnAuthorization_RedirectsToHttpsEndpoint_WithSpecifiedStatusCodeAndrequireHttpsPermanentOption(bool? permanent, bool requireHttpsPermanent)
        {
            var requestContext = new DefaultHttpContext();
            requestContext.RequestServices = CreateServices(null, requireHttpsPermanent);
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = "GET";
            
            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsAttribute();
            if (permanent.HasValue)
            {
                attr.Permanent = permanent.Value; 
            };

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            var result = Assert.IsType<RedirectResult>(authContext.Result);
            Assert.Equal(permanent ?? requireHttpsPermanent, result.Permanent);
        }

        private class CustomRequireHttpsAttribute : RequireHttpsAttribute
        {
            protected override void HandleNonHttpsRequest(AuthorizationFilterContext filterContext)
            {
                filterContext.Result = new StatusCodeResult(StatusCodes.Status404NotFound);
            }
        }

        private static AuthorizationFilterContext CreateAuthorizationContext(HttpContext ctx)
        {
            var actionContext = new ActionContext(ctx, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new IFilterMetadata[0]);
        }

        private static IServiceProvider CreateServices(int? sslPort = null, bool requireHttpsPermanent = false)
        {
            var options = Options.Create(new MvcOptions());
            options.Value.SslPort = sslPort;
            options.Value.RequireHttpsPermanent = requireHttpsPermanent;

            var services = new ServiceCollection();
            services.AddSingleton<IOptions<MvcOptions>>(options);

            return services.BuildServiceProvider();
        }
    }
}
