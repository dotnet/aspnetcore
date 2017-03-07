// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Xunit;
using MemoryPool = Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure.MemoryPool;
using MemoryPoolBlock = Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure.MemoryPoolBlock;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class MemoryPoolIteratorTests : IDisposable
    {
        private readonly MemoryPool _pool;

        public MemoryPoolIteratorTests()
        {
            _pool = new MemoryPool();
        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        [Fact]
        public void Put()
        {
            var blocks = new MemoryPoolBlock[4];
            for (var i = 0; i < 4; ++i)
            {
                blocks[i] = _pool.Lease();
                blocks[i].End += 16;

                for (var j = 0; j < blocks.Length; ++j)
                {
                    blocks[i].Array[blocks[i].Start + j] = 0x00;
                }

                if (i != 0)
                {
                    blocks[i - 1].Next = blocks[i];
                }
            }

            // put FF at first block's head
            var head = blocks[0].GetIterator();
            Assert.True(head.Put(0xFF));

            // data is put at correct position
            Assert.Equal(0xFF, blocks[0].Array[blocks[0].Start]);
            Assert.Equal(0x00, blocks[0].Array[blocks[0].Start + 1]);

            // iterator is moved to next byte after put
            Assert.Equal(1, head.Index - blocks[0].Start);

            for (var i = 0; i < 14; ++i)
            {
                // move itr to the end of the block 0
                head.Take();
            }

            // write to the end of block 0
            Assert.True(head.Put(0xFE));
            Assert.Equal(0xFE, blocks[0].Array[blocks[0].End - 1]);
            Assert.Equal(0x00, blocks[1].Array[blocks[1].Start]);

            // put data across the block link
            Assert.True(head.Put(0xFD));
            Assert.Equal(0xFD, blocks[1].Array[blocks[1].Start]);
            Assert.Equal(0x00, blocks[1].Array[blocks[1].Start + 1]);

            // paint every block
            head = blocks[0].GetIterator();
            for (var i = 0; i < 64; ++i)
            {
                Assert.True(head.Put((byte)i), $"Fail to put data at {i}.");
            }

            // Can't put anything by the end
            Assert.ThrowsAny<InvalidOperationException>(() => head.Put(0xFF));

            for (var i = 0; i < 4; ++i)
            {
                _pool.Return(blocks[i]);
            }
        }

        [Fact]
        public async Task PeekArraySegment()
        {
            using (var pipeFactory = new PipeFactory())
            {
                // Arrange
                var pipe = pipeFactory.Create();
                var buffer = pipe.Writer.Alloc();
                buffer.Append(ReadableBuffer.Create(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
                await buffer.FlushAsync();
                
                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] {0, 1, 2, 3, 4, 5, 6, 7}, result);

                pipe.Writer.Complete();
                pipe.Reader.Complete();
            }
        }

        [Fact]
        public async Task PeekArraySegmentAtEndOfDataReturnsDefaultArraySegment()
        {
            using (var pipeFactory = new PipeFactory())
            {
                // Arrange
                var pipe = pipeFactory.Create();
                pipe.Writer.Complete();

                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                // Assert.Equals doesn't work since xunit tries to access the underlying array.
                Assert.True(default(ArraySegment<byte>).Equals(result));

                pipe.Reader.Complete();
            }
        }

        [Fact]
        public async Task PeekArraySegmentAtBlockBoundary()
        {
            using (var pipeFactory = new PipeFactory())
            {
                var pipe = pipeFactory.Create();
                var buffer = pipe.Writer.Alloc();
                buffer.Append(ReadableBuffer.Create(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
                buffer.Append(ReadableBuffer.Create(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 }));
                await buffer.FlushAsync();

                // Act
                var result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, result);

                // Act
                // Advance past the data in the first block
                var readResult = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                pipe.Reader.Advance(readResult.Buffer.Move(readResult.Buffer.Start, 8));
                result = await pipe.Reader.PeekAsync();

                // Assert
                Assert.Equal(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 }, result);

                pipe.Writer.Complete();
                pipe.Reader.Complete();
            }

        }


        [Fact]
        public void EmptyIteratorBehaviourIsValid()
        {
            const byte byteCr = (byte)'\n';
            var end = default(MemoryPoolIterator);
            
            Assert.True(default(MemoryPoolIterator).IsDefault);
            Assert.True(default(MemoryPoolIterator).IsEnd);

            default(MemoryPoolIterator).CopyFrom(default(ArraySegment<byte>));
            default(MemoryPoolIterator).CopyFromAscii("");
            Assert.ThrowsAny<InvalidOperationException>(() => default(MemoryPoolIterator).Put(byteCr));
            Assert.ThrowsAny<InvalidOperationException>(() => default(MemoryPoolIterator).GetLength(end));
        }

        [Theory]
        [InlineData("a", "a", 1)]
        [InlineData("ab", "a...", 1)]
        [InlineData("abcde", "abcde", 5)]
        [InlineData("abcde", "abcd...", 4)]
        [InlineData("abcde", "abcde", 6)]
        public void TestGetAsciiStringEscaped(string input, string expected, int maxChars)
        {
                // Arrange
            var buffer = new Span<byte>(Encoding.ASCII.GetBytes(input));

            // Act
            var result = buffer.GetAsciiStringEscaped(maxChars);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CorrectContentLengthsOutput()
        {
            using (var pool = new MemoryPool())
            {
                var block = pool.Lease();
                try
                {
                    for (var i = 0u; i <= 9u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)(i + '0'));
                        Assert.Equal(block.End, block.Start + 1);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 10u; i <= 99u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 2);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 100u; i <= 999u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 100) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)(((i % 100) / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 2], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 3);
                        Assert.Equal(iter.Index, block.End);
                    }
                    for (var i = 1000u; i <= 9999u; i++)
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(i);

                        Assert.Equal(block.Array[block.Start], (byte)((i / 1000) + '0'));
                        Assert.Equal(block.Array[block.Start + 1], (byte)(((i % 1000) / 100) + '0'));
                        Assert.Equal(block.Array[block.Start + 2], (byte)(((i % 100) / 10) + '0'));
                        Assert.Equal(block.Array[block.Start + 3], (byte)((i % 10) + '0'));

                        Assert.Equal(block.End, block.Start + 4);
                        Assert.Equal(iter.Index, block.End);
                    }
                    {
                        block.Reset();
                        var iter = new MemoryPoolIterator(block);
                        iter.CopyFromNumeric(ulong.MaxValue);

                        var outputBytes = Encoding.ASCII.GetBytes(ulong.MaxValue.ToString("0"));

                        for (var i = 0; i < outputBytes.Length; i++)
                        {
                            Assert.Equal(block.Array[block.Start + i], outputBytes[i]);
                        }

                        Assert.Equal(block.End, block.Start + outputBytes.Length);
                        Assert.Equal(iter.Index, block.End);
                    }
                }
                finally
                {
                    pool.Return(block);
                }
            }
        }

        public static IEnumerable<object[]> SeekByteLimitData
        {
            get
            {
                var vectorSpan = Vector<byte>.Count;

                // string input, char seek, int limit, int expectedBytesScanned, int expectedReturnValue
                var data = new List<object[]>();

                // Non-vector inputs

                data.Add(new object[] { "hello, world", 'h', 12, 1, 'h' });
                data.Add(new object[] { "hello, world", ' ', 12, 7, ' ' });
                data.Add(new object[] { "hello, world", 'd', 12, 12, 'd' });
                data.Add(new object[] { "hello, world", '!', 12, 12, -1 });
                data.Add(new object[] { "hello, world", 'h', 13, 1, 'h' });
                data.Add(new object[] { "hello, world", ' ', 13, 7, ' ' });
                data.Add(new object[] { "hello, world", 'd', 13, 12, 'd' });
                data.Add(new object[] { "hello, world", '!', 13, 12, -1 });
                data.Add(new object[] { "hello, world", 'h', 5, 1, 'h' });
                data.Add(new object[] { "hello, world", 'o', 5, 5, 'o' });
                data.Add(new object[] { "hello, world", ',', 5, 5, -1 });
                data.Add(new object[] { "hello, world", 'd', 5, 5, -1 });
                data.Add(new object[] { "abba", 'a', 4, 1, 'a' });
                data.Add(new object[] { "abba", 'b', 4, 2, 'b' });

                // Vector inputs

                // Single vector, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan), 'b', vectorSpan, vectorSpan, -1 });
                // Two vectors, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2), 'b', vectorSpan * 2, vectorSpan * 2, -1 });
                // Two vectors plus non vector length (thus hitting slow path too), no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2 + vectorSpan / 2), 'b', vectorSpan * 2 + vectorSpan / 2, vectorSpan * 2 + vectorSpan / 2, -1 });

                // For each input length from 1/2 to 3 1/2 vector spans in 1/2 vector span increments...
                for (var length = vectorSpan / 2; length <= vectorSpan * 3 + vectorSpan / 2; length += vectorSpan / 2)
                {
                    // ...place the seek char at vector and input boundaries...
                    for (var i = Math.Min(vectorSpan - 1, length - 1); i < length; i += ((i + 1) % vectorSpan == 0) ? 1 : Math.Min(i + (vectorSpan - 1), length - 1))
                    {
                        var input = new StringBuilder(new string('a', length));
                        input[i] = 'b';

                        // ...and check with a seek byte limit before, at, and past the seek char position...
                        for (var limitOffset = -1; limitOffset <= 1; limitOffset++)
                        {
                            var limit = (i + 1) + limitOffset;

                            if (limit >= i + 1)
                            {
                                // ...that Seek() succeeds when the seek char is within that limit...
                                data.Add(new object[] { input.ToString(), 'b', limit, i + 1, 'b' });
                            }
                            else
                            {
                                // ...and fails when it's not.
                                data.Add(new object[] { input.ToString(), 'b', limit, Math.Min(length, limit), -1 });
                            }
                        }
                    }
                }

                return data;
            }
        }

        public static IEnumerable<object[]> SeekIteratorLimitData
        {
            get
            {
                var vectorSpan = Vector<byte>.Count;

                // string input, char seek, char limitAt, int expectedReturnValue
                var data = new List<object[]>();

                // Non-vector inputs

                data.Add(new object[] { "hello, world", 'h', 'd', 'h' });
                data.Add(new object[] { "hello, world", ' ', 'd', ' ' });
                data.Add(new object[] { "hello, world", 'd', 'd', 'd' });
                data.Add(new object[] { "hello, world", '!', 'd', -1 });
                data.Add(new object[] { "hello, world", 'h', 'w', 'h' });
                data.Add(new object[] { "hello, world", 'o', 'w', 'o' });
                data.Add(new object[] { "hello, world", 'r', 'w', -1 });
                data.Add(new object[] { "hello, world", 'd', 'w', -1 });

                // Vector inputs

                // Single vector, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan), 'b', 'b', -1 });
                // Two vectors, no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2), 'b', 'b', -1 });
                // Two vectors plus non vector length (thus hitting slow path too), no seek char in input, expect failure
                data.Add(new object[] { new string('a', vectorSpan * 2 + vectorSpan / 2), 'b', 'b', -1 });

                // For each input length from 1/2 to 3 1/2 vector spans in 1/2 vector span increments...
                for (var length = vectorSpan / 2; length <= vectorSpan * 3 + vectorSpan / 2; length += vectorSpan / 2)
                {
                    // ...place the seek char at vector and input boundaries...
                    for (var i = Math.Min(vectorSpan - 1, length - 1); i < length; i += ((i + 1) % vectorSpan == 0) ? 1 : Math.Min(i + (vectorSpan - 1), length - 1))
                    {
                        var input = new StringBuilder(new string('a', length));
                        input[i] = 'b';

                        // ...along with sentinel characters to seek the limit iterator to...
                        input[i - 1] = 'A';
                        if (i < length - 1) input[i + 1] = 'B';

                        // ...and check that Seek() succeeds with a limit iterator at or past the seek char position...
                        data.Add(new object[] { input.ToString(), 'b', 'b', 'b' });
                        if (i < length - 1) data.Add(new object[] { input.ToString(), 'b', 'B', 'b' });

                        // ...and fails with a limit iterator before the seek char position.
                        data.Add(new object[] { input.ToString(), 'b', 'A', -1 });
                    }
                }

                return data;
            }
        }
    }
}