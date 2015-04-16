// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.WebEncoders;
#if DNX451
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

#if DNX451
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

            var runtimeEnvironmentMock = new Mock<IRuntimeEnvironment>(MockBehavior.Strict);
            runtimeEnvironmentMock.Setup(r => r.OperatingSystem).Returns("Windows");
            runtimeEnvironmentMock.Setup(r => r.RuntimeArchitecture).Returns("x64");
            runtimeEnvironmentMock.Setup(r => r.RuntimeType).Returns("clr");
            runtimeEnvironmentMock.Setup(r => r.RuntimeVersion).Returns("1.0.0");

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
                next,
                new RuntimeInfoPageOptions(),
                libraryManagerMock.Object,
                runtimeEnvironmentMock.Object);

            // Act
            var model = middleware.CreateRuntimeInfoModel();

            // Assert
            Assert.Equal("1.0.0", model.Version);
            Assert.Equal("Windows", model.OperatingSystem);
            Assert.Equal("clr", model.RuntimeType);
            Assert.Equal("x64", model.RuntimeArchitecture);
            Assert.Same(libraries, model.References);
        }

        [Fact]
        public async void Invoke_WithNonMatchingPath_IgnoresRequest()
        {
            // Arrange
            var libraryManagerMock = new Mock<ILibraryManager>(MockBehavior.Strict);
            var runtimeEnvironmentMock = new Mock<IRuntimeEnvironment>(MockBehavior.Strict);

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
               next,
               new RuntimeInfoPageOptions(),
               libraryManagerMock.Object,
               runtimeEnvironmentMock.Object);

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

            var runtimeEnvironmentMock = new Mock<IRuntimeEnvironment>(MockBehavior.Strict);
            runtimeEnvironmentMock.Setup(r => r.OperatingSystem).Returns("Windows");
            runtimeEnvironmentMock.Setup(r => r.RuntimeArchitecture).Returns("x64");
            runtimeEnvironmentMock.Setup(r => r.RuntimeType).Returns("clr");
            runtimeEnvironmentMock.Setup(r => r.RuntimeVersion).Returns("1.0.0");

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
                next,
                new RuntimeInfoPageOptions(),
                libraryManagerMock.Object,
                runtimeEnvironmentMock.Object);

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
                    .SetupGet(c => c.ApplicationServices)
                    .Returns(() => null);

                // Act
                await middleware.Invoke(contextMock.Object);

                // Assert
                string response = Encoding.UTF8.GetString(buffer);

                Assert.Contains("<p>Runtime Version: 1.0.0</p>", response);
                Assert.Contains("<p>Operating System: Windows</p>", response);
                Assert.Contains("<p>Runtime Architecture: x64</p>", response);
                Assert.Contains("<p>Runtime Type: clr</p>", response);
                Assert.Contains("<td>LibInfo1</td>", response);
                Assert.Contains("<td>1.0.0-beta1</td>", response);
                Assert.Contains("<td>Path1</td>", response);
                Assert.Contains("<td>LibInfo2</td>", response);
                Assert.Contains("<td>1.0.0-beta2</td>", response);
                Assert.Contains("<td>Path2</td>", response);
            }
        }

        [Fact]
        public async void Invoke_WithMatchingPath_ReturnsInfoPage_UsingCustomHtmlEncoder()
        {
            // Arrange
            var libraryManagerMock = new Mock<ILibraryManager>(MockBehavior.Strict);
            libraryManagerMock.Setup(l => l.GetLibraries()).Returns(new ILibraryInformation[] {
                        new FakeLibraryInformation() { Name ="LibInfo1", Version = "1.0.0-beta1", Path = "Path1" },
                    });

            var runtimeEnvironmentMock = new Mock<IRuntimeEnvironment>(MockBehavior.Strict);
            runtimeEnvironmentMock.Setup(r => r.OperatingSystem).Returns("Windows");
            runtimeEnvironmentMock.Setup(r => r.RuntimeArchitecture).Returns("x64");
            runtimeEnvironmentMock.Setup(r => r.RuntimeType).Returns("clr");
            runtimeEnvironmentMock.Setup(r => r.RuntimeVersion).Returns("1.0.0");

            RequestDelegate next = _ =>
            {
                return Task.FromResult<object>(null);
            };

            var middleware = new RuntimeInfoMiddleware(
                next,
                new RuntimeInfoPageOptions(),
                libraryManagerMock.Object,
                runtimeEnvironmentMock.Object);

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
                    .SetupGet(c => c.ApplicationServices)
                    .Returns(new ServiceCollection().
                                AddInstance<IHtmlEncoder>(new CustomHtmlEncoder()).
                                BuildServiceProvider());

                // Act
                await middleware.Invoke(contextMock.Object);

                // Assert
                string response = Encoding.UTF8.GetString(buffer);

                Assert.True(response.Contains("<td>[LibInfo1]</td>"));
                Assert.True(response.Contains("<td>[1.0.0-beta1]</td>"));
                Assert.True(response.Contains("<td>[Path1]</td>"));
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

            public IEnumerable<AssemblyName> LoadableAssemblies
            {
                get
                {
                    throw new NotImplementedException("Should not be needed by this middleware");
                }
            }
        }

        private class CustomHtmlEncoder : IHtmlEncoder
        {
            public string HtmlEncode(string value)
            {
                return "[" + value + "]";
            }

            public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
            {
                throw new NotImplementedException();
            }

            public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
            {
                throw new NotImplementedException();
            }
        }
    }
}
