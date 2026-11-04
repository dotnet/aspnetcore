// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Moq;

namespace Microsoft.AspNetCore.WebUtilities;

public class HttpRequestStreamReaderTest
{
    private static readonly char[] CharData = new char[]
    {
        char.MinValue,
        char.MaxValue,
        '\t',
        ' ',
        '$',
        '@',
        '#',
        '\0',
        '\v',
        '\'',
        '\u3190',
        '\uC3A0',
        'A',
        '5',
        '\r',
        '\uFE70',
        '-',
        ';',
        '\r',
        '\n',
        'T',
        '3',
        '\n',
        'K',
        '\u00E6',
    };

    [Fact]
    public static async Task ReadToEndAsync()
    {
        // Arrange
        var reader = new HttpRequestStreamReader(GetLargeStream(), Encoding.UTF8);

        var result = await reader.ReadToEndAsync();

        Assert.Equal(5000, result.Length);
    }

    [Fact]
    public static async Task ReadToEndAsync_Reads_Asynchronously()
    {
        // Arrange
        var stream = new AsyncOnlyStreamWrapper(GetLargeStream());
        var reader = new HttpRequestStreamReader(stream, Encoding.UTF8);
        var streamReader = new StreamReader(GetLargeStream());
        string expected = await streamReader.ReadToEndAsync();

        // Act
        var actual = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void TestRead()
    {
        // Arrange
        var reader = CreateReader();

        // Act & Assert
        for (var i = 0; i < CharData.Length; i++)
        {
            var tmp = reader.Read();
            Assert.Equal((int)CharData[i], tmp);
        }
    }

    [Fact]
    public static void TestPeek()
    {
        // Arrange
        var reader = CreateReader();

        // Act & Assert
        for (var i = 0; i < CharData.Length; i++)
        {
            var peek = reader.Peek();
            Assert.Equal((int)CharData[i], peek);

            reader.Read();
        }
    }

    [Fact]
    public static void EmptyStream()
    {
        // Arrange
        var reader = new HttpRequestStreamReader(new MemoryStream(), Encoding.UTF8);
        var buffer = new char[10];

        // Act
        var read = reader.Read(buffer, 0, 1);

        // Assert
        Assert.Equal(0, read);
    }

    [Fact]
    public static void Read_ReadAllCharactersAtOnce()
    {
        // Arrange
        var reader = CreateReader();
        var chars = new char[CharData.Length];

        // Act
        var read = reader.Read(chars, 0, chars.Length);

        // Assert
        Assert.Equal(chars.Length, read);
        for (var i = 0; i < CharData.Length; i++)
        {
            Assert.Equal(CharData[i], chars[i]);
        }
    }

    [Fact]
    public static async Task ReadAsync_ReadInTwoChunks()
    {
        // Arrange
        var reader = CreateReader();
        var chars = new char[CharData.Length];

        // Act
        var read = await reader.ReadAsync(chars, 4, 3);

        // Assert
        Assert.Equal(3, read);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(CharData[i], chars[i + 4]);
        }
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_ReadMultipleLines(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var reader = CreateReader();
        var valueString = new string(CharData);

        // Act & Assert
        var data = await action(reader);
        Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

        data = await action(reader);
        Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

        data = await action(reader);
        Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

        data = await action(reader);
        Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_ReadWithNoNewlines(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var reader = CreateReader();
        var valueString = new string(CharData);
        var temp = new char[10];

        // Act
        reader.Read(temp, 0, 1);
        var data = await action(reader);

        // Assert
        Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_MultipleContinuousLines(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("\n\n\r\r\n\r");
        writer.Flush();
        stream.Position = 0;

        var reader = new HttpRequestStreamReader(stream, Encoding.UTF8);

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            var data = await action(reader);
            Assert.Equal(string.Empty, data);
        }

        var eof = await action(reader);
        Assert.Null(eof);
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_CarriageReturnAndLineFeedAcrossBufferBundaries(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("123456789\r\nfoo");
        writer.Flush();
        stream.Position = 0;

        var reader = new HttpRequestStreamReader(stream, Encoding.UTF8, 10);

        // Act & Assert
        var data = await action(reader);
        Assert.Equal("123456789", data);

        data = await action(reader);
        Assert.Equal("foo", data);

        var eof = await action(reader);
        Assert.Null(eof);
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_EOF(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var stream = new MemoryStream();
        var reader = new HttpRequestStreamReader(stream, Encoding.UTF8);

        // Act & Assert
        var eof = await action(reader);
        Assert.Null(eof);
    }

    [Theory]
    [MemberData(nameof(ReadLineData))]
    public static async Task ReadLine_NewLineOnly(Func<HttpRequestStreamReader, Task<string>> action)
    {
        // Arrange
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("\r\n");
        writer.Flush();
        stream.Position = 0;

        var reader = new HttpRequestStreamReader(stream, Encoding.UTF8);

        // Act & Assert
        var empty = await action(reader);
        Assert.Equal(string.Empty, empty);
    }

    [Fact]
    public static void Read_Span_ReadAllCharactersAtOnce()
    {
        // Arrange
        var reader = CreateReader();
        var chars = new char[CharData.Length];
        var span = new Span<char>(chars);

        // Act
        var read = reader.Read(span);

        // Assert
        Assert.Equal(chars.Length, read);
        for (var i = 0; i < CharData.Length; i++)
        {
            Assert.Equal(CharData[i], chars[i]);
        }
    }

    [Fact]
    public static void Read_Span_WithMoreDataThanInternalBufferSize()
    {
        // Arrange
        var reader = CreateReader(10);
        var chars = new char[CharData.Length];
        var span = new Span<char>(chars);

        // Act
        var read = reader.Read(span);

        // Assert
        Assert.Equal(chars.Length, read);
        for (var i = 0; i < CharData.Length; i++)
        {
            Assert.Equal(CharData[i], chars[i]);
        }
    }

    [Fact]
    public static async Task ReadAsync_Memory_ReadAllCharactersAtOnce()
    {
        // Arrange
        var reader = CreateReader();
        var chars = new char[CharData.Length];
        var memory = new Memory<char>(chars);

        // Act
        var read = await reader.ReadAsync(memory);

        // Assert
        Assert.Equal(chars.Length, read);
        for (var i = 0; i < CharData.Length; i++)
        {
            Assert.Equal(CharData[i], chars[i]);
        }
    }

    [Fact]
    public static async Task ReadAsync_Memory_WithMoreDataThanInternalBufferSize()
    {
        // Arrange
        var reader = CreateReader(10);
        var chars = new char[CharData.Length];
        var memory = new Memory<char>(chars);

        // Act
        var read = await reader.ReadAsync(memory);

        // Assert
        Assert.Equal(chars.Length, read);
        for (var i = 0; i < CharData.Length; i++)
        {
            Assert.Equal(CharData[i], chars[i]);
        }
    }

    [Theory]
    [MemberData(nameof(HttpRequestNullData))]
    public static void NullInputsInConstructor_ExpectArgumentNullException(Stream stream, Encoding encoding, ArrayPool<byte> bytePool, ArrayPool<char> charPool)
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var httpRequestStreamReader = new HttpRequestStreamReader(stream, encoding, 1, bytePool, charPool);
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
        mockStream.Setup(m => m.CanRead).Returns(false);
        Assert.Throws<ArgumentException>(() =>
        {
            var httpRequestStreamReader = new HttpRequestStreamReader(mockStream.Object, Encoding.UTF8, 1, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        });
    }

    [Theory]
    [MemberData(nameof(HttpRequestDisposeData))]
    public static void StreamDisposed_ExpectedObjectDisposedException(Action<HttpRequestStreamReader> action)
    {
        var httpRequestStreamReader = new HttpRequestStreamReader(new MemoryStream(), Encoding.UTF8, 10, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        httpRequestStreamReader.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            action(httpRequestStreamReader);
        });
    }

    [Theory]
    [MemberData(nameof(HttpRequestDisposeDataAsync))]
    public static async Task StreamDisposed_ExpectObjectDisposedExceptionAsync(Func<HttpRequestStreamReader, Task> action)
    {
        var httpRequestStreamReader = new HttpRequestStreamReader(new MemoryStream(), Encoding.UTF8, 10, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
        httpRequestStreamReader.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => action(httpRequestStreamReader));
    }

    private static HttpRequestStreamReader CreateReader()
    {
        MemoryStream stream = CreateStream();
        return new HttpRequestStreamReader(stream, Encoding.UTF8);
    }

    private static HttpRequestStreamReader CreateReader(int bufferSize)
    {
        MemoryStream stream = CreateStream();
        return new HttpRequestStreamReader(stream, Encoding.UTF8, bufferSize);
    }

    private static MemoryStream CreateStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(CharData);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream GetSmallStream()
    {
        var testData = new byte[] { 72, 69, 76, 76, 79 };
        return new MemoryStream(testData);
    }

    private static MemoryStream GetLargeStream()
    {
        var testData = new byte[] { 72, 69, 76, 76, 79 };
        // System.Collections.Generic.

        var data = new List<byte>();
        for (var i = 0; i < 1000; i++)
        {
            data.AddRange(testData);
        }

        return new MemoryStream(data.ToArray());
    }

    public static IEnumerable<object?[]> HttpRequestNullData()
    {
        yield return new object?[] { null, Encoding.UTF8, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), null, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), Encoding.UTF8, null, ArrayPool<char>.Shared };
        yield return new object?[] { new MemoryStream(), Encoding.UTF8, ArrayPool<byte>.Shared, null };
    }

    public static IEnumerable<object[]> HttpRequestDisposeData()
    {
        yield return new object[] { new Action<HttpRequestStreamReader>((httpRequestStreamReader) =>
            {
                 var res = httpRequestStreamReader.Read();
            })};
        yield return new object[] { new Action<HttpRequestStreamReader>((httpRequestStreamReader) =>
            {
                 var res = httpRequestStreamReader.Read(new char[10], 0, 1);
            })};
        yield return new object[] { new Action<HttpRequestStreamReader>((httpRequestStreamReader) =>
            {
                 var res = httpRequestStreamReader.Read(new Span<char>(new char[10], 0, 1));
            })};

        yield return new object[] { new Action<HttpRequestStreamReader>((httpRequestStreamReader) =>
            {
                var res = httpRequestStreamReader.Peek();
            })};

    }

    public static IEnumerable<object[]> HttpRequestDisposeDataAsync()
    {
        yield return new object[] { new Func<HttpRequestStreamReader, Task>(async (httpRequestStreamReader) =>
            {
                 await httpRequestStreamReader.ReadAsync(new char[10], 0, 1);
            })};
        yield return new object[] { new Func<HttpRequestStreamReader, Task>(async (httpRequestStreamReader) =>
            {
                 await httpRequestStreamReader.ReadAsync(new Memory<char>(new char[10], 0, 1));
            })};
    }

    public static IEnumerable<object[]> ReadLineData()
    {
        yield return new object[] { new Func<HttpRequestStreamReader, Task<string?>>((httpRequestStreamReader) =>
                 Task.FromResult(httpRequestStreamReader.ReadLine())
            )};
        yield return new object[] { new Func<HttpRequestStreamReader, Task<string?>>((httpRequestStreamReader) =>
                 httpRequestStreamReader.ReadLineAsync()
            )};
    }

    private class AsyncOnlyStreamWrapper : Stream
    {
        private readonly Stream _inner;

        public AsyncOnlyStreamWrapper(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush()
        {
            throw SyncOperationForbiddenException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw SyncOperationForbiddenException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw SyncOperationForbiddenException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            _inner.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            return _inner.DisposeAsync();
        }

        private Exception SyncOperationForbiddenException()
        {
            return new InvalidOperationException("The stream cannot be accessed synchronously");
        }
    }
}
