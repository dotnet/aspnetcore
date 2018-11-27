// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Cors
{
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
            Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);
        }
    }
}
