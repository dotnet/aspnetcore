// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Tests
{
    public class RuntimeInfoMiddlewareTest
    {
        private const string DefaultPath = "/runtimeinfo";

        [Fact]
        public void DefaultPageOptions_HasDefaultPath()
        {
            // Arrange & act
            var options = new RuntimeInfoPageOptions();

            // Assert
            Assert.Equal(DefaultPath, options.Path.Value);
        }

        [Fact]
        public async void Invoke_WithNonMatchingPath_IgnoresRequest()
        {
            // Arrange
            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
               next,
               Options.Create(new RuntimeInfoPageOptions()));

            var contextMock = new Mock<HttpContext>(MockBehavior.Strict);
            contextMock
                .SetupGet(c => c.Request.Path)
                .Returns(new PathString("/nonmatchingpath"));

            // Act
            await middleware.Invoke(contextMock.Object);

            // Assert
            contextMock.VerifyGet(c => c.Request.Path, Times.Once());
        }

        [Fact]
        public async void Invoke_WithMatchingPath_ReturnsInfoPage()
        {
            // Arrange
            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };
            var runtimeEnvironment = PlatformServices.Default.Runtime;

            var middleware = new RuntimeInfoMiddleware(
                next,
                Options.Create(new RuntimeInfoPageOptions()));

            var buffer = new byte[4096];
            using (var responseStream = new MemoryStream(buffer))
            {
                var contextMock = new Mock<HttpContext>(MockBehavior.Strict);
                contextMock
                    .SetupGet(c => c.Request.Path)
                    .Returns(new PathString("/runtimeinfo"));
                contextMock
                    .SetupGet(c => c.Response.Body)
                    .Returns(responseStream);
                contextMock
                    .SetupGet(c => c.RequestServices)
                    .Returns(() => null);

                // Act
                await middleware.Invoke(contextMock.Object);

                // Assert
                var response = Encoding.UTF8.GetString(buffer);

                Assert.Contains($"<p>Runtime Version: {runtimeEnvironment.RuntimeVersion}</p>", response);
                Assert.Contains($"<p>Operating System: {runtimeEnvironment.OperatingSystem}</p>", response);
                Assert.Contains($"<p>Runtime Architecture: {runtimeEnvironment.RuntimeArchitecture}</p>", response);
                Assert.Contains($"<p>Runtime Type: {runtimeEnvironment.RuntimeType}</p>", response);
            }
        }
    }
}
