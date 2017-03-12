// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class NonDisposableStreamTest
    {
#if NET46
        [Fact]
        public void InnerStreamIsOpenOnClose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var nonDisposableStream = new NonDisposableStream(innerStream);

            // Act
            nonDisposableStream.Close();

            // Assert
            Assert.True(innerStream.CanRead);
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnClose()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var nonDisposableStream = new NonDisposableStream(stream);

            // Act & Assert
            nonDisposableStream.Close();
        }
#elif NETCOREAPP2_0
#else
#error The target frameworks need to be updated
#endif

        [Fact]
        public void InnerStreamIsOpenOnDispose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var nonDisposableStream = new NonDisposableStream(innerStream);

            // Act
            nonDisposableStream.Dispose();

            // Assert
            Assert.True(innerStream.CanRead);
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnDispose()
        {
            var stream = FlushReportingStream.GetThrowingStream();
            var nonDisposableStream = new NonDisposableStream(stream);

            // Act & Assert
            nonDisposableStream.Dispose();
        }

        [Fact]
        public void InnerStreamIsNotFlushedOnFlush()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var nonDisposableStream = new NonDisposableStream(stream);

            // Act & Assert
            nonDisposableStream.Flush();
        }

        [Fact]
        public async Task InnerStreamIsNotFlushedOnFlushAsync()
        {
            // Arrange
            var stream = FlushReportingStream.GetThrowingStream();

            var nonDisposableStream = new NonDisposableStream(stream);

            // Act & Assert
            await nonDisposableStream.FlushAsync();
        }
    }
}
