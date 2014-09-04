// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class DelegatingStreamTests
    {
        [Fact]
        public void InnerStreamIsOpenOnClose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var delegatingStream = new DelegatingStream(innerStream);

            // Act
            delegatingStream.Close();

            // Assert
            Assert.True(innerStream.CanRead);
        }

        [Fact]
        public void InnerStreamIsOpenOnDispose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var delegatingStream = new DelegatingStream(innerStream);

            // Act
            delegatingStream.Dispose();

            // Assert
            Assert.True(innerStream.CanRead);
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnDispose()
        {
            var stream = FlushReportingStream.GetThrowingStream();
            var delegatingStream = new DelegatingStream(stream);

            // Act & Assert
            delegatingStream.Dispose();
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnClose()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var delegatingStream = new DelegatingStream(stream);

            // Act & Assert
            delegatingStream.Close();
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnFlush()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var delegatingStream = new DelegatingStream(stream);

            // Act & Assert
            delegatingStream.Flush();
        }

        [Fact]
        public async Task InnerStreamIsNotFlushedOnFlushAsync()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var delegatingStream = new DelegatingStream(stream);

            // Act & Assert
            await delegatingStream.FlushAsync();
        }
    }
}
#endif
