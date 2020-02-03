// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace Microsoft.AspNetCore.WebUtilities
{
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

        [Fact]
        public static void ReadLine_ReadMultipleLines()
        {
            // Arrange
            var reader = CreateReader();
            var valueString = new string(CharData);

            // Act & Assert
            var data = reader.ReadLine();
            Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

            data = reader.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

            data = reader.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

            data = reader.ReadLine();
            Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
        }

        [Fact]
        public static void ReadLine_ReadWithNoNewlines()
        {
            // Arrange
            var reader = CreateReader();
            var valueString = new string(CharData);
            var temp = new char[10];

            // Act
            reader.Read(temp, 0, 1);
            var data = reader.ReadLine();

            // Assert
            Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
        }

        [Fact]
        public static async Task ReadLineAsync_MultipleContinuousLines()
        {
            // Arrange
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("\n\n\r\r\n");
            writer.Flush();
            stream.Position = 0;

            var reader = new HttpRequestStreamReader(stream, Encoding.UTF8);

            // Act & Assert
            for (var i = 0; i < 4; i++)
            {
                var data = await reader.ReadLineAsync();
                Assert.Equal(string.Empty, data);
            }

            var eol = await reader.ReadLineAsync();
            Assert.Null(eol);
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
        public async static Task ReadAsync_Memory_ReadAllCharactersAtOnce()
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
        public async static Task ReadAsync_Memory_WithMoreDataThanInternalBufferSize()
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

        [Fact]
        public static async Task StreamDisposed_ExpectObjectDisposedExceptionAsync()
        {
            var httpRequestStreamReader = new HttpRequestStreamReader(new MemoryStream(), Encoding.UTF8, 10, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
            httpRequestStreamReader.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            {
                return httpRequestStreamReader.ReadAsync(new char[10], 0, 1);
            });
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

        public static IEnumerable<object[]> HttpRequestNullData()
        {
            yield return new object[] { null, Encoding.UTF8, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
            yield return new object[] { new MemoryStream(), null, ArrayPool<byte>.Shared, ArrayPool<char>.Shared };
            yield return new object[] { new MemoryStream(), Encoding.UTF8, null, ArrayPool<char>.Shared };
            yield return new object[] { new MemoryStream(), Encoding.UTF8, ArrayPool<byte>.Shared, null };
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
                var res = httpRequestStreamReader.Peek();
            })};

        }
    }
}
