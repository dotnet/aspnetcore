// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Runtime;
#if ASPNET50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Tests
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

#if ASPNET50
        [Fact]
        public void CreateRuntimeInfoModel_GetsTheVersionAndAllPackages()
        {
            // Arrage
            var libraries = new ILibraryInformation[] {
                new FakeLibraryInformation() { Name ="LibInfo1", Path = "Path1" },
                new FakeLibraryInformation() { Name ="LibInfo2", Path = "Path2" },
            };

            var libraryManagerMock = new Mock<ILibraryManager>(MockBehavior.Strict);
            libraryManagerMock.Setup(l => l.GetLibraries()).Returns(libraries);

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
                next,
                new RuntimeInfoPageOptions(),
                libraryManagerMock.Object);

            // Act
            var model = middleware.CreateRuntimeInfoModel();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(model.Version));
            Assert.Same(libraries, model.References);
        }

        [Fact]
        public async void Invoke_WithNonMatchingPath_IgnoresRequest()
        {
            // Arrange
            var libraryManagerMock = new Mock<ILibraryManager>(MockBehavior.Strict);

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
               next,
               new RuntimeInfoPageOptions(),
               libraryManagerMock.Object);

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
            var libraryManagerMock = new Mock<ILibraryManager>(MockBehavior.Strict);
            libraryManagerMock.Setup(l => l.GetLibraries()).Returns(new ILibraryInformation[] {
                        new FakeLibraryInformation() { Name ="LibInfo1", Version = "1.0.0-beta1", Path = "Path1" },
                        new FakeLibraryInformation() { Name ="LibInfo2", Version = "1.0.0-beta2", Path = "Path2" },
                    });

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
                next,
                new RuntimeInfoPageOptions(),
                libraryManagerMock.Object);

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

                // Act
                await middleware.Invoke(contextMock.Object);

                // Assert
                string response = Encoding.UTF8.GetString(buffer);

                Assert.True(response.Contains("<td>LibInfo1</td>"));
                Assert.True(response.Contains("<td>1.0.0-beta1</td>"));
                Assert.True(response.Contains("<td>Path1</td>"));
                Assert.True(response.Contains("<td>LibInfo2</td>"));
                Assert.True(response.Contains("<td>1.0.0-beta2</td>"));
                Assert.True(response.Contains("<td>Path2</td>"));
            }
        }
#endif

        private class FakeLibraryInformation : ILibraryInformation
        {
            public string Name { get; set; }

            public string Version { get; set; }

            public string Path { get; set; }

            public IEnumerable<string> Dependencies
            {
                get
                {
                    throw new NotImplementedException("Should not be needed by this middleware");
                }
            }

            public string Type
            {
                get
                {
                    throw new NotImplementedException("Should not be needed by this middleware");
                }
            }
        }
    }
}