// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpResponseStreamWriterTest
    {
        [Fact]
        public async Task DoesNotWriteBOM()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var encodingWithBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var writer = new HttpResponseStreamWriter(memoryStream, encodingWithBOM);
            var expectedData = new byte[] { 97, 98, 99, 100 }; // without BOM

            // Act
            using (writer)
            {
                await writer.WriteAsync("abcd");
            }

            // Assert
            Assert.Equal(expectedData, memoryStream.ToArray());
        }

#if DNX451
        [Fact]
        public async Task DoesNotFlush_UnderlyingStream_OnClosingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Close();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
        }
#endif

        [Fact]
        public async Task DoesNotFlush_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
        }

#if DNX451
        [Fact]
        public async Task DoesNotClose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Close();

            // Assert
            Assert.Equal(0, stream.CloseCallCount);
        }
#endif

        [Fact]
        public async Task DoesNotDispose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello world");
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.DisposeCallCount);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_OnClose(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
#if DNX451
            writer.Close();
#else
            writer.Dispose();
#endif

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_OnDispose(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Fact]
        public void NoDataWritten_Flush_DoesNotFlushUnderlyingStream()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            writer.Flush();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void FlushesBuffer_ButNotStream_OnFlush(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            writer.Write(new string('a', byteLength));

            var expectedWriteCount = Math.Ceiling((double)byteLength / HttpResponseStreamWriter.DefaultBufferSize);

            // Act
            writer.Flush();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(expectedWriteCount, stream.WriteCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Fact]
        public async Task NoDataWritten_FlushAsync_DoesNotFlushUnderlyingStream()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_ButNotStream_OnFlushAsync(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            var expectedWriteCount = Math.Ceiling((double)byteLength / HttpResponseStreamWriter.DefaultBufferSize);

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(expectedWriteCount, stream.WriteAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void WriteChar_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                for (var i = 0; i < byteLength; i++)
                {
                    writer.Write('a');
                }
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void WriteCharArray_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                writer.Write((new string('a', byteLength)).ToCharArray());
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task WriteCharAsync_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                for (var i = 0; i < byteLength; i++)
                {
                    await writer.WriteAsync('a');
                }
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task WriteCharArrayAsync_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                await writer.WriteAsync((new string('a', byteLength)).ToCharArray());
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData("你好世界", "utf-16")]
#if !DNXCORE50
        // CoreCLR does not like shift_jis as an encoding.
        [InlineData("こんにちは世界", "shift_jis")]
#endif
        [InlineData("హలో ప్రపంచ", "iso-8859-1")]
        [InlineData("வணக்கம் உலக", "utf-32")]
        public async Task WritesData_InExpectedEncoding(string data, string encodingName)
        {
            // Arrange
            var encoding = Encoding.GetEncoding(encodingName);
            var expectedBytes = encoding.GetBytes(data);
            var stream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(stream, encoding);

            // Act
            using (writer)
            {
                await writer.WriteAsync(data);
            }

            // Assert
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        [Theory]
        [InlineData('ん', 1023, "utf-8")]
        [InlineData('ん', 1024, "utf-8")]
        [InlineData('ん', 1050, "utf-8")]
        [InlineData('你', 1023, "utf-16")]
        [InlineData('你', 1024, "utf-16")]
        [InlineData('你', 1050, "utf-16")]
#if !DNXCORE50
        // CoreCLR does not like shift_jis as an encoding.
        [InlineData('こ', 1023, "shift_jis")]
        [InlineData('こ', 1024, "shift_jis")]
        [InlineData('こ', 1050, "shift_jis")]
#endif
        [InlineData('హ', 1023, "iso-8859-1")]
        [InlineData('హ', 1024, "iso-8859-1")]
        [InlineData('హ', 1050, "iso-8859-1")]
        [InlineData('வ', 1023, "utf-32")]
        [InlineData('வ', 1024, "utf-32")]
        [InlineData('வ', 1050, "utf-32")]
        public async Task WritesData_OfDifferentLength_InExpectedEncoding(
            char character,
            int charCount,
            string encodingName)
        {
            // Arrange
            var encoding = Encoding.GetEncoding(encodingName);
            string data = new string(character, charCount);
            var expectedBytes = encoding.GetBytes(data);
            var stream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(stream, encoding);

            // Act
            using (writer)
            {
                await writer.WriteAsync(data);
            }

            // Assert
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        // None of the code in HttpResponseStreamWriter differs significantly when using pooled buffers.
        //
        // This test effectively verifies that things are correctly constructed and disposed. Pooled buffers
        // throw on the finalizer thread if not disposed, so that's why it's complicated.
        [Fact]
        public void HttpResponseStreamWriter_UsingPooledBuffers()
        {
            // Arrange
            var encoding = Encoding.UTF8;
            var stream = new MemoryStream();

            var expectedBytes = encoding.GetBytes("Hello, World!");

            using (var writer = new HttpResponseStreamWriter(
                stream,
                encoding,
                1024,
                ArrayPool<byte>.Shared,
                ArrayPool<char>.Shared))
            {
                // Act
                writer.Write("Hello, World!");
            }

            // Assert
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        private class TestMemoryStream : MemoryStream
        {
            public int FlushCallCount { get; private set; }

            public int FlushAsyncCallCount { get; private set; }

            public int CloseCallCount { get; private set; }

            public int DisposeCallCount { get; private set; }

            public int WriteCallCount { get; private set; }

            public int WriteAsyncCallCount { get; private set; }

            public override void Flush()
            {
                FlushCallCount++;
                base.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                FlushAsyncCallCount++;
                return base.FlushAsync(cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteCallCount++;
                base.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                WriteAsyncCallCount++;
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

#if DNX451
            public override void Close()
            {
                CloseCallCount++;
                base.Close();
            }
#endif

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                base.Dispose(disposing);
            }
        }
    }
}
