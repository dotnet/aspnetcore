// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PagedBufferedTextWriterTest
    {
        private static readonly char[] Content;

        static PagedBufferedTextWriterTest()
        {
            Content = new char[4 * PagedBufferedTextWriter.PageSize];
            for (var i = 0; i < Content.Length; i++)
            {
                Content[i] = (char)((i % 26) + 'A');
            }
        }

        [Fact]
        public async Task Write_Char()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // Act
            for (var i = 0; i < Content.Length; i++)
            {
                writer.Write(Content[i]);
            }

            await writer.FlushAsync();

            // Assert
            Assert.Equal<char>(Content, inner.ToString().ToCharArray());
        }

        [Fact]
        public async Task Write_CharArray_Null()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // Act
            writer.Write((char[])null);

            await writer.FlushAsync();

            // Assert
            Assert.Empty(inner.ToString());
        }

        [Fact]
        public async Task Write_CharArray()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // These numbers chosen to hit boundary conditions in buffer lengths
            Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
            var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

            // Act
            var offset = 0;
            foreach (var chunkSize in chunkSizes)
            {
                var chunk = new char[chunkSize];
                for (var j = 0; j < chunkSize; j++)
                {
                    chunk[j] = Content[offset + j];
                }

                writer.Write(chunk);
                offset += chunkSize;
            }

            await writer.FlushAsync();

            // Assert
            var array = inner.ToString().ToCharArray();
            for (var i = 0; i < Content.Length; i++)
            {
                Assert.Equal(Content[i], array[i]);
            }

            Assert.Equal<char>(Content, inner.ToString().ToCharArray());
        }

        [Fact]
        public void Write_CharArray_Bounded_Null()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // Act & Assert
            Assert.Throws<ArgumentNullException>("buffer", () => writer.Write(null, 0, 0));
        }

        [Fact]
        public async Task Write_CharArray_Bounded()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // These numbers chosen to hit boundary conditions in buffer lengths
            Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
            var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

            // Act
            var offset = 0;
            foreach (var chunkSize in chunkSizes)
            {
                writer.Write(Content, offset, chunkSize);
                offset += chunkSize;
            }

            await writer.FlushAsync();

            // Assert
            Assert.Equal<char>(Content, inner.ToString().ToCharArray());
        }

        [Fact]
        public async Task Write_String_Null()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // Act
            writer.Write((string)null);

            await writer.FlushAsync();

            // Assert
            Assert.Empty(inner.ToString());
        }

        [Fact]
        public async Task Write_String()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            // These numbers chosen to hit boundary conditions in buffer lengths
            Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
            var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

            // Act
            var offset = 0;
            foreach (var chunkSize in chunkSizes)
            {
                var chunk = new string(Content, offset, chunkSize);
                writer.Write(chunk);
                offset += chunkSize;
            }

            await writer.FlushAsync();

            // Assert
            Assert.Equal<char>(Content, inner.ToString().ToCharArray());
        }

        [Fact]
        public async Task FlushAsync_ReturnsPages()
        {
            // Arrange
            var pool = new TestArrayPool();
            var inner = new StringWriter();

            var writer = new PagedBufferedTextWriter(pool, inner);

            for (var i = 0; i < Content.Length; i++)
            {
                writer.Write(Content[i]);
            }

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(3, pool.Returned.Count);
        }

        private class TestArrayPool : ArrayPool<char>
        {
            public IList<char[]> Returned { get; } = new List<char[]>();

            public override char[] Rent(int minimumLength)
            {
                return new char[minimumLength];
            }

            public override void Return(char[] buffer, bool clearArray = false)
            {
                Returned.Add(buffer);
            }
        }
    }
}
