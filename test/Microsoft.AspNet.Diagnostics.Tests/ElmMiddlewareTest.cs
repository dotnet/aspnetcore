// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
#if ASPNET50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Tests
{
    public class ElmMiddlewareTest
    {
        private const string DefaultPath = "/Elm";

        [Fact]
        public void DefaultPageOptions_HasDefaultPath()
        {
            // Arrange & act
            var options = new ElmOptions();

            // Assert
            Assert.Equal(DefaultPath, options.Path.Value);
        }

#if ASPNET50
        [Fact]
        public async void Invoke_WithNonMatchingPath_IgnoresRequest()
        {
            // Arrange
            var elmStore = new ElmStore();
            var factory = new LoggerFactory();
            var optionsMock = new Mock<IOptions<ElmOptions>>();
            optionsMock
                .SetupGet(o => o.Options)
                .Returns(new ElmOptions());
            factory.AddProvider(new ElmLoggerProvider(elmStore, optionsMock.Object.Options));

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var captureMiddleware = new ElmCaptureMiddleware(
                next,
                factory,
                optionsMock.Object);
            var pageMiddleware = new ElmPageMiddleware(
                next,
                optionsMock.Object,
                elmStore);

            var contextMock = GetMockContext("/nonmatchingpath");

            // Act
            await captureMiddleware.Invoke(contextMock.Object);
            await pageMiddleware.Invoke(contextMock.Object);

            // Assert
            // Request.Query is used by the ElmPageMiddleware to parse the query parameters
            contextMock.VerifyGet(c => c.Request.Query, Times.Never());
        }

        [Fact]
        public async void Invoke_WithMatchingPath_FulfillsRequest()
        {
            // Arrange
            var elmStore = new ElmStore();
            var factory = new LoggerFactory();
            var optionsMock = new Mock<IOptions<ElmOptions>>();
            optionsMock
                .SetupGet(o => o.Options)
                .Returns(new ElmOptions());
            factory.AddProvider(new ElmLoggerProvider(elmStore, optionsMock.Object.Options));

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var captureMiddleware = new ElmCaptureMiddleware(
                next,
                factory,
                optionsMock.Object);
            var pageMiddleware = new ElmPageMiddleware(
                next,
                optionsMock.Object,
                elmStore);
            var contextMock = GetMockContext("/Elm");

            using (var responseStream = new MemoryStream())
            {
                contextMock
                    .SetupGet(c => c.Response.Body)
                    .Returns(responseStream);

                // Act
                await captureMiddleware.Invoke(contextMock.Object);
                await pageMiddleware.Invoke(contextMock.Object);

                string response = Encoding.UTF8.GetString(responseStream.ToArray());

                // Assert
                contextMock.VerifyGet(c => c.Request.Query, Times.AtLeastOnce());
                Assert.True(response.Contains("<title>ASP.NET Logs</title>"));
            }
        }

        [Fact]
        public async void Invoke_BadRequestShowsError()
        {
            // Arrange
            var elmStore = new ElmStore();
            var factory = new LoggerFactory();
            var optionsMock = new Mock<IOptions<ElmOptions>>();
            optionsMock
                .SetupGet(o => o.Options)
                .Returns(new ElmOptions());
            factory.AddProvider(new ElmLoggerProvider(elmStore, optionsMock.Object.Options));

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var captureMiddleware = new ElmCaptureMiddleware(
                next,
                factory,
                optionsMock.Object);
            var pageMiddleware = new ElmPageMiddleware(
                next,
                optionsMock.Object,
                elmStore);
            var contextMock = GetMockContext("/Elm/666");

            using (var responseStream = new MemoryStream())
            {
                contextMock
                    .SetupGet(c => c.Response.Body)
                    .Returns(responseStream);

                // Act
                await captureMiddleware.Invoke(contextMock.Object);
                await pageMiddleware.Invoke(contextMock.Object);

                string response = Encoding.UTF8.GetString(responseStream.ToArray());

                // Assert
                contextMock.VerifyGet(c => c.Request.Query, Times.AtLeastOnce());
                Assert.True(response.Contains("Invalid Request Id"));
            }
        }

        private Mock<HttpContext> GetMockContext(string path)
        {
            var contextMock = new Mock<HttpContext>(MockBehavior.Strict);
            contextMock
                .SetupGet(c => c.Request.Path)
                .Returns(new PathString(path));
            contextMock
                .SetupGet(c => c.Request.Host)
                .Returns(new HostString("localhost"));
            contextMock
                .SetupGet(c => c.Request.ContentType)
                .Returns("");
            contextMock
                .SetupGet(c => c.Request.Scheme)
                .Returns("http");
            contextMock
                .SetupGet(c => c.Request.Scheme)
                .Returns("http");
            contextMock
                .SetupGet(c => c.Response.StatusCode)
                .Returns(200);
            contextMock
                .SetupGet(c => c.Response.Body)
                .Returns(new Mock<Stream>().Object);
            contextMock
                .SetupGet(c => c.User)
                .Returns(new ClaimsPrincipal());
            contextMock
                .SetupGet(c => c.Request.Method)
                .Returns("GET");
            contextMock
                .SetupGet(c => c.Request.Protocol)
                .Returns("HTTP/1.1");
            contextMock
                .SetupGet(c => c.Request.Headers)
                .Returns(new Mock<IHeaderDictionary>().Object);
            contextMock
                .SetupGet(c => c.Request.QueryString)
                .Returns(new QueryString());
            contextMock
                .SetupGet(c => c.Request.Query)
                .Returns(new Mock<IReadableStringCollection>().Object);
            contextMock
                .SetupGet(c => c.Request.Cookies)
                .Returns(new Mock<IReadableStringCollection>().Object);
            contextMock
                .Setup(c => c.Request.GetFormAsync(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.FromResult(new Mock<IReadableStringCollection>().Object));

            return contextMock;
        }
#endif
    }
}