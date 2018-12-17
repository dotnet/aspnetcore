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
        public void Configure_AssignsInvalidModelStateResponseFactory()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup();
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Same(ApiBehaviorOptionsSetup.DefaultFactory, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void Configure_AddsClientErrorMappings()
        {
            // Arrange
            var expected = new[] { 400, 401, 403, 404, 406, 409, 415, 422, };
            var optionsSetup = new ApiBehaviorOptionsSetup();
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(expected, options.ClientErrorMapping.Keys);
        }

        [Fact]
        public void PostConfigure_SetProblemDetailsModelStateResponseFactory()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup();
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);
            optionsSetup.PostConfigure(string.Empty, options);

            // Assert
            Assert.Same(ApiBehaviorOptionsSetup.ProblemDetailsFactory, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void PostConfigure_DoesNotSetProblemDetailsFactory_IfValueWasModified()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup();
            var options = new ApiBehaviorOptions();
            Func<ActionContext, IActionResult> expected = _ => null;

            // Act
            optionsSetup.Configure(options);
            // This is equivalent to user code updating the value via ConfigureOptions
            options.InvalidModelStateResponseFactory = expected;
            optionsSetup.PostConfigure(string.Empty, options);

            // Assert
            Assert.Same(expected, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void ProblemDetailsInvalidModelStateResponse_ReturnsBadRequestWithProblemDetails()
        {
            // Arrange
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "42" },
            };

            // Act
            var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(actionContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, badRequest.ContentTypes.OrderBy(c => c));

            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal(400, problemDetails.Status);
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

                // Act
                var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(actionContext);

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

            // Act
            var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(actionContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal("42", problemDetails.Extensions["traceId"]);
        }
    }
}
