// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ApiBehaviorOptionsSetupTest
    {
        [Fact]
        public void Configure_AddsClientErrorMappings()
        {
            // Arrange
            var expected = new[] { 400, 401, 403, 404, 406, 409, 415, 422, 500, };
            var optionsSetup = new ApiBehaviorOptionsSetup();
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(expected, options.ClientErrorMapping.Keys);
        }

        [Fact]
        public void ProblemDetailsInvalidModelStateResponse_ReturnsBadRequestWithProblemDetails()
        {
            // Arrange
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "42" },
            };

            var factory = GetInvalidModelStateResponseFactory();

            // Act
            var result = factory(actionContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, badRequest.ContentTypes.OrderBy(c => c));

            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal(400, problemDetails.Status);
            Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problemDetails.Type);
        }

        [Fact]
        public void ProblemDetailsInvalidModelStateResponse_SetsTraceId()
        {
            // Arrange
            using (new ActivityReplacer())
            {
                var actionContext = new ActionContext
                {
                    HttpContext = new DefaultHttpContext { TraceIdentifier = "42" },
                };
                var factory = GetInvalidModelStateResponseFactory();

                // Act
                var result = factory(actionContext);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
                Assert.Equal(Activity.Current.Id, problemDetails.Extensions["traceId"]);
            }
        }

        [Fact]
        public void ProblemDetailsInvalidModelStateResponse_SetsTraceIdFromRequest_IfActivityIsNull()
        {
            // Arrange
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "42" },
            };
            var factory = GetInvalidModelStateResponseFactory();

            // Act
            var result = factory(actionContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal("42", problemDetails.Extensions["traceId"]);
        }

        private static Func<ActionContext, IActionResult> GetInvalidModelStateResponseFactory()
        {
            var options = new ApiBehaviorOptions();
            var setup = new ApiBehaviorOptionsSetup();

            setup.Configure(options);

            var factory = options.InvalidModelStateResponseFactory;
            return factory;
        }
    }
}
