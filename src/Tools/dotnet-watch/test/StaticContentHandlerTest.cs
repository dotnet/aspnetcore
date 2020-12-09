// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class StaticContentHandlerTest
    {
        [Fact]
        public async ValueTask TryHandleFileAction_WritesUpdateCssMessage()
        {
            // Arrange
            var server = new Mock<BrowserRefreshServer>(NullReporter.Singleton);
            byte[]? writtenBytes = null;
            server.Setup(s => s.SendMessage(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Callback((byte[] bytes, CancellationToken cts) =>
                {
                    writtenBytes = bytes;
                });

            // Act
            await StaticContentHandler.TryHandleFileAction(server.Object, new FileItem("Test.css", FileKind.StaticFile, "content/Test.css"), default);

            // Assert
            Assert.NotNull(writtenBytes);
            var writtenString = Encoding.UTF8.GetString(writtenBytes!);
            Assert.Equal("UpdateCSS||content/Test.css", writtenString);
        }

        [Fact]
        public async ValueTask TryHandleFileAction_CausesBrowserRefreshForNonCssFile()
        {
            // Arrange
            var server = new Mock<BrowserRefreshServer>(NullReporter.Singleton);
            byte[]? writtenBytes = null;
            server.Setup(s => s.SendMessage(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Callback((byte[] bytes, CancellationToken cts) =>
                {
                    writtenBytes = bytes;
                });

            // Act
            await StaticContentHandler.TryHandleFileAction(server.Object, new FileItem("Test.js", FileKind.StaticFile, "content/Test.js"), default);

            // Assert
            Assert.NotNull(writtenBytes);
            var writtenString = Encoding.UTF8.GetString(writtenBytes!);
            Assert.Equal("Reload", writtenString);
        }
    }
}
