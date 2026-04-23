// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Moq;

namespace Microsoft.AspNetCore.WebUtilities;

public class HttpResponseStreamWriterTest
{
    private const int DefaultCharacterChunkSize = HttpResponseStreamWriter.DefaultBufferSize;

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
        writer.Dispose();

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
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize - 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize * 2)]
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
    public async Task FlushWriteThrows_DontFlushInDispose(int byteLength)
    {
        // Arrange
        var stream = new TestMemoryStream() { ThrowOnWrite = true };
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync(new string('a', byteLength));
        await Assert.ThrowsAsync<IOException>(() => writer.FlushAsync());

        // Act
        writer.Dispose();

        // Assert
        Assert.Equal(1, stream.WriteAsyncCallCount);
        Assert.Equal(0, stream.WriteCallCount);
        Assert.Equal(0, stream.FlushCallCount);
        Assert.Equal(0, stream.FlushAsyncCallCount);
        Assert.Equal(0, stream.Length);
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
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    public void WriteReadOnlySpanChar_WritesToStream(int byteLength)
    {
        // Arrange
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        // Act
        using (writer)
        {
            var array = new string('a', byteLength).ToCharArray();
            var span = new ReadOnlySpan<char>(array);
            writer.Write(span);
        }

        // Assert
        Assert.Equal(byteLength, stream.Length);
    }

    [Theory]
    [InlineData(1022, "\n")]
    [InlineData(1023, "\n")]
    [InlineData(1024, "\n")]
    [InlineData(1050, "\n")]
    [InlineData(2047, "\n")]
    [InlineData(2048, "\n")]
    [InlineData(1021, "\r\n")]
    [InlineData(1022, "\r\n")]
    [InlineData(1023, "\r\n")]
    [InlineData(1024, "\r\n")]
    [InlineData(1050, "\r\n")]
    [InlineData(2046, "\r\n")]
    [InlineData(2048, "\r\n")]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, "\r\n")]
    public void WriteLineReadOnlySpanChar_WritesToStream(int byteLength, string newLine)
    {
        // Arrange
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        writer.NewLine = newLine;
        // Act
        using (writer)
        {
            var array = new string('a', byteLength).ToCharArray();
            var span = new ReadOnlySpan<char>(array);
            writer.WriteLine(span);
        }

        // Assert
        Assert.Equal(byteLength + newLine.Length, stream.Length);
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
    [InlineData(0)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1050)]
    [InlineData(2048)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    public async Task WriteReadOnlyMemoryAsync_WritesToStream(int byteLength)
    {
        // Arrange
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        // Act
        using (writer)
        {
            var array = new string('a', byteLength).ToCharArray();
            var memory = new ReadOnlyMemory<char>(array);
            await writer.WriteAsync(memory);
        }

        // Assert
        Assert.Equal(byteLength, stream.Length);
    }

    [Fact]
    public async Task WriteReadOnlyMemoryAsync_TokenCanceled_ReturnsCanceledTask()
    {
        // Arrange
        var stream = new TestMemoryStream();
        using var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        var memory = new ReadOnlyMemory<char>(new char[] { 'a' });
        var cancellationToken = new CancellationToken(true);

        // Act
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await writer.WriteAsync(memory, cancellationToken));

        // Assert
        Assert.Equal(0, stream.Length);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1022, 1)]
    [InlineData(1023, 1)]
    [InlineData(1024, 1)]
    [InlineData(1050, 1)]
    [InlineData(2047, 1)]
    [InlineData(2048, 1)]
    [InlineData(1021, 2)]
    [InlineData(1022, 2)]
    [InlineData(1023, 2)]
    [InlineData(1024, 2)]
    [InlineData(1024, 1023)]
    [InlineData(1024, 1024)]
    [InlineData(1024, 1050)]
    [InlineData(1050, 2)]
    [InlineData(2046, 2)]
    [InlineData(2048, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, HttpResponseStreamWriter.DefaultBufferSize)]
    public async Task WriteLineReadOnlyMemoryAsync_WritesToStream(int byteLength, int newLineLength)
    {
        // Arrange
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            var array = new string('a', byteLength).ToCharArray();
            var memory = new ReadOnlyMemory<char>(array);
            await writer.WriteLineAsync(memory);
        }

        // Assert
        Assert.Equal(byteLength + newLineLength, stream.Length);
    }

    [Fact]
    public async Task WriteLineReadOnlyMemoryAsync_TokenCanceled_ReturnsCanceledTask()
    {
        // Arrange
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        var memory = new ReadOnlyMemory<char>(new char[] { 'a' });
        var cancellationToken = new CancellationToken(true);

        // Act
        using (writer)
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await writer.WriteLineAsync(memory, cancellationToken));
        }

        // Assert
        Assert.Equal(0, stream.Length);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1022, 1)]
    [InlineData(1023, 1)]
    [InlineData(1024, 1)]
    [InlineData(1050, 1)]
    [InlineData(2047, 1)]
    [InlineData(2048, 1)]
    [InlineData(1021, 2)]
    [InlineData(1022, 2)]
    [InlineData(1023, 2)]
    [InlineData(1024, 2)]
    [InlineData(1024, 1023)]
    [InlineData(1024, 1024)]
    [InlineData(1024, 1050)]
    [InlineData(1050, 2)]
    [InlineData(2046, 2)]
    [InlineData(2048, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, HttpResponseStreamWriter.DefaultBufferSize)]
    public async Task WriteLineAsyncCharArray_WritesToStream(int charCount, int newLineLength)
    {
        // Arrange
        var content = new char[charCount];
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content, 0, charCount);
        }

        // Assert
        Assert.Equal(charCount + newLineLength, stream.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    public async Task WriteLineAsyncCharArray_OnlyWritesNewLineToStream_ForNullArgument(int newLineLength)
    {
        // Arrange
        char[]? content = null;
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content!, 0, 0);
        }

        // Assert
        Assert.Equal(newLineLength, stream.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    public async Task WriteLineAsyncCharArray_OnlyWritesNewLineToStream_ForZeroCount(int newLineLength)
    {
        // Arrange
        var content = new char[1];
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content, 0, 0);
        }

        // Assert
        Assert.Equal(newLineLength, stream.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize - 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize)]
    public async Task WriteLineAsyncChar_WritesToStream(int newLineLength)
    {
        // Arrange
        var content = 'a';
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content);
        }

        // Assert
        Assert.Equal(newLineLength + 1, stream.Length);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1022, 1)]
    [InlineData(1023, 1)]
    [InlineData(1024, 1)]
    [InlineData(1050, 1)]
    [InlineData(2047, 1)]
    [InlineData(2048, 1)]
    [InlineData(1021, 2)]
    [InlineData(1022, 2)]
    [InlineData(1023, 2)]
    [InlineData(1024, 2)]
    [InlineData(1024, 1023)]
    [InlineData(1024, 1024)]
    [InlineData(1024, 1050)]
    [InlineData(1050, 2)]
    [InlineData(2046, 2)]
    [InlineData(2048, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 1)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, 2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1, HttpResponseStreamWriter.DefaultBufferSize)]
    public async Task WriteLineAsyncString_WritesToStream(int charCount, int newLineLength)
    {
        // Arrange
        var content = new string('a', charCount);
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content);
        }

        // Assert
        Assert.Equal(charCount + newLineLength, stream.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
    public async Task WriteLineAsyncString_OnlyWritesNewLineToStream_ForNullArgument(int newLineLength)
    {
        // Arrange
        string? content = null;
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.NewLine = new string('\n', newLineLength);

        // Act
        using (writer)
        {
            await writer.WriteLineAsync(content);
        }

        // Assert
        Assert.Equal(newLineLength, stream.Length);
    }

    [Theory]
    [InlineData("你好世界", "utf-16")]
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

    [Theory]
    [InlineData(DefaultCharacterChunkSize)]
    [InlineData(DefaultCharacterChunkSize * 2)]
    [InlineData(DefaultCharacterChunkSize * 3)]
    public async Task HttpResponseStreamWriter_WritesDataCorrectly_ForCharactersHavingSurrogatePairs(int characterSize)
    {
        // Arrange
        // Here "𐐀" (called Deseret Long I) actually represents 2 characters. Try to make this character split across
        // the boundary
        var content = new string('a', characterSize - 1) + "𐐀";
        var stream = new TestMemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.Unicode);

        // Act
        await writer.WriteAsync(content);
        await writer.FlushAsync();

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(stream, Encoding.Unicode);
        var actualContent = await streamReader.ReadToEndAsync();
        Assert.Equal(content, actualContent);
    }

    [Theory]
    [MemberData(nameof(HttpResponseStreamWriterData))]
    public static void NullInputsInConstructor_ExpectArgumentNullException(Stream stream, Encoding encoding, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var httpRequestStreamReader = new HttpResponseStreamWriter(stream, encoding, 1, bytePool, charPool);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static void NegativeOrZeroBufferSize_ExpectArgumentOutOfRangeException(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var httpRequestStreamReader = new HttpRequestStreamReader(new MemoryStream(), Encoding.UTF8, size, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        });
    }

    [Fact]
    public static void StreamCannotRead_ExpectArgumentException()
    {
        var mockStream = new Mock<Stream>();
        mockStream.Setup(m => m.CanWrite).Returns(false);
        Assert.Throws<ArgumentException>(() =>
        {
            var httpRequestStreamReader = new HttpRequestStreamReader(mockStream.Object, Encoding.UTF8, 1, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        });
    }

    [Theory]
    [MemberData(nameof(HttpResponseDisposeData))]
    public static void StreamDisposed_ExpectedObjectDisposedException(Action<HttpResponseStreamWriter> action)
    {
        var httpResponseStreamWriter = new HttpResponseStreamWriter(new MemoryStream(), Encoding.UTF8, 10, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        httpResponseStreamWriter.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            action(httpResponseStreamWriter);
        });
    }

    [Theory]
    [MemberData(nameof(HttpResponseDisposeDataAsync))]
    public static async Task StreamDisposed_ExpectedObjectDisposedExceptionAsync(Func<HttpResponseStreamWriter, Task> function)
    {
        var httpResponseStreamWriter = new HttpResponseStreamWriter(new MemoryStream(), Encoding.UTF8, 10, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        httpResponseStreamWriter.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
        {
            return function(httpResponseStreamWriter);
        });
    }

    private class TestMemoryStream : MemoryStream
    {
        public int FlushCallCount { get; private set; }

        public int FlushAsyncCallCount { get; private set; }

        public int CloseCallCount { get; private set; }

        public int DisposeCallCount { get; private set; }

        public int WriteCallCount { get; private set; }

        public int WriteAsyncCallCount { get; private set; }

        public bool ThrowOnWrite { get; set; }

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
            if (ThrowOnWrite)
            {
                throw new IOException("Test IOException");
            }
            base.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            WriteAsyncCallCount++;
            if (ThrowOnWrite)
            {
                throw new IOException("Test IOException");
            }
            return base.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            base.Dispose(disposing);
        }
    }

    public static IEnumerable<object?[]> HttpResponseStreamWriterData()
    {
        yield return new object?[] { null, Encoding.UTF8, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), null, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), Encoding.UTF8, null, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), Encoding.UTF8, ArrayPool<byte>.Shared, null };
    }

    public static IEnumerable<object[]> HttpResponseDisposeData()
    {
        yield return new object[] { new Action<HttpResponseStreamWriter>((httpResponseStreamWriter) =>
            {
                 httpResponseStreamWriter.Write('a');
            })};
        yield return new object[] { new Action<HttpResponseStreamWriter>((httpResponseStreamWriter) =>
            {
                 httpResponseStreamWriter.Write(new char[] { 'a', 'b' }, 0, 1);
            })};
        yield return new object[] { new Action<HttpResponseStreamWriter>((httpResponseStreamWriter) =>
            {
                httpResponseStreamWriter.Write("hello");
            })};
        yield return new object[] { new Action<HttpResponseStreamWriter>((httpResponseStreamWriter) =>
            {
                httpResponseStreamWriter.Write(new ReadOnlySpan<char>(new char[] { 'a', 'b' }));
            })};

        yield return new object[] { new Action<HttpResponseStreamWriter>((httpResponseStreamWriter) =>
            {
                httpResponseStreamWriter.Flush();
            })};
    }

    public static IEnumerable<object[]> HttpResponseDisposeDataAsync()
    {
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteAsync('a');
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteAsync(new char[] { 'a', 'b' }, 0, 1);
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteAsync("hello");
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteAsync(new ReadOnlyMemory<char>(new char[] { 'a', 'b' }));
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteLineAsync(new ReadOnlyMemory<char>(new char[] { 'a', 'b' }));
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteLineAsync(new char[] { 'a', 'b' }, 0, 1);
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteLineAsync('a');
            })};
        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.WriteLineAsync("hello");
            })};

        yield return new object[] { new Func<HttpResponseStreamWriter, Task>(async (httpResponseStreamWriter) =>
            {
                await httpResponseStreamWriter.FlushAsync();
            })};
    }

    [Fact]
    public void WriteUtf8_WritesBytesDirectlyToStream()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        var utf8Bytes = "<h1>Hello World</h1>"u8;

        writer.WriteUtf8(utf8Bytes);
        writer.Flush();

        Assert.Equal(utf8Bytes.ToArray(), stream.ToArray());
    }

    [Fact]
    public async Task WriteUtf8Async_WritesBytesDirectlyToStream()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        var utf8Bytes = "<h1>Hello World</h1>"u8.ToArray();

        await writer.WriteUtf8Async(utf8Bytes);
        await writer.FlushAsync();

        Assert.Equal(utf8Bytes, stream.ToArray());
    }

    [Fact]
    public void WriteUtf8_FlushesCharBufferFirst_MaintainsOrdering()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        writer.Write("Hello ");
        writer.WriteUtf8("<b>World</b>"u8);
        writer.Flush();

        var output = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello <b>World</b>", output);
    }

    [Fact]
    public async Task WriteUtf8Async_FlushesCharBufferFirst_MaintainsOrdering()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync("Hello ");
        await writer.WriteUtf8Async("<b>World</b>"u8.ToArray());
        await writer.FlushAsync();

        var output = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello <b>World</b>", output);
    }

    [Fact]
    public void WriteUtf8_MixedCharsAndBytes_InterleavedCorrectly()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        writer.Write("<html>");
        writer.WriteUtf8("<head>"u8);
        writer.Write("<title>Test</title>");
        writer.WriteUtf8("</head>"u8);
        writer.Write("<body></body></html>");
        writer.Flush();

        var output = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("<html><head><title>Test</title></head><body></body></html>", output);
    }

    [Fact]
    public void WriteUtf8_WithEmptySpan_DoesNothing()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

        writer.WriteUtf8(ReadOnlySpan<byte>.Empty);
        writer.Flush();

        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void WriteUtf8_ThrowsForNonUtf8Encoding()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.Unicode);

        Assert.Throws<InvalidOperationException>(() => writer.WriteUtf8("<p>test</p>"u8));
    }

    [Fact]
    public async Task WriteUtf8Async_ThrowsForNonUtf8Encoding()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.Unicode);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.WriteUtf8Async("<p>test</p>"u8.ToArray()));
    }

    [Fact]
    public void WriteUtf8_ThrowsWhenDisposed()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteUtf8("<p>test</p>"u8));
    }

    [Fact]
    public async Task WriteUtf8Async_ThrowsWhenDisposed()
    {
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        writer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => writer.WriteUtf8Async("<p>test</p>"u8.ToArray()));
    }

    [Fact]
    public void WriteUtf8_BytesNotReEncoded()
    {
        // Verify the bytes go to the stream byte-for-byte without re-encoding
        var stream = new MemoryStream();
        var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
        var originalBytes = "Héllo Wörld — 日本語"u8.ToArray();

        writer.WriteUtf8(originalBytes);
        writer.Flush();

        Assert.Equal(originalBytes, stream.ToArray());
    }
}
