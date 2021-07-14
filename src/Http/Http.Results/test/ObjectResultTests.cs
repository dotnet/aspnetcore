// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class ObjectResultTests
    {
        [Fact]
        public async Task ObjectResult_ExecuteAsync_WithNullValue_Works()
        {
            // Arrange
            var result = new ObjectResult(value: null, 411);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(411, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ObjectResult_ExecuteAsync_SetsStatusCode()
        {
            // Arrange
            var result = new ObjectResult("Hello", 407);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(407, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ObjectResult_ExecuteAsync_JsonSerializesBody()
        {
            // Arrange
            var result = new ObjectResult("Hello", 407);
            var stream = new MemoryStream();
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
                Response =
                {
                    Body = stream,
                },
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal("\"Hello\"", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task ExecuteAsync_UsesDefaults_ForProblemDetails()
        {
            // Arrange
            var details = new ProblemDetails();

            var result = new ObjectResult(details);
            var stream = new MemoryStream();
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
                Response =
                {
                    Body = stream,
                },
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
            stream.Position = 0;
            var responseDetails = JsonSerializer.Deserialize<ProblemDetails>(stream);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", responseDetails.Type);
            Assert.Equal("An error occurred while processing your request.", responseDetails.Title);
            Assert.Equal(StatusCodes.Status500InternalServerError, responseDetails.Status);
        }

        [Fact]
        public async Task ExecuteAsync_UsesDefaults_ForValidationProblemDetails()
        {
            // Arrange
            var details = new HttpValidationProblemDetails();

            var result = new ObjectResult(details);
            var stream = new MemoryStream();
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
                Response =
                {
                    Body = stream,
                },
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
            stream.Position = 0;
            var responseDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(stream);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", responseDetails.Type);
            Assert.Equal("One or more validation errors occurred.", responseDetails.Title);
            Assert.Equal(StatusCodes.Status400BadRequest, responseDetails.Status);
        }

        [Fact]
        public async Task ExecuteAsync_SetsProblemDetailsStatus_ForValidationProblemDetails()
        {
            // Arrange
            var details = new HttpValidationProblemDetails();

            var result = new ObjectResult(details, StatusCodes.Status422UnprocessableEntity);
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
        }

        [Fact]
        public async Task ExecuteAsync_GetsStatusCodeFromProblemDetails()
        {
            // Arrange
            var details = new ProblemDetails { Status = StatusCodes.Status413RequestEntityTooLarge, };

            var result = new ObjectResult(details);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, details.Status.Value);
            Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, result.StatusCode.Value);
            Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ExecuteAsync_UsesStatusCodeFromResultTypeForProblemDetails()
        {
            // Arrange
            var details = new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, };

            var result = new BadRequestObjectResult(details);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            return services.BuildServiceProvider();
        }
    }
}
