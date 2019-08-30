// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    public class ArrayBuilderTest
    {
        private readonly TestArrayPool<int> ArrayPool = new TestArrayPool<int>();

        [Fact]
        public void Append_SingleItem()
        {
            // Arrange
            var value = 7;
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(value);

            // Assert
            Assert.Equal(1, builder.Count);
            Assert.Equal(value, builder.Buffer[0]);
        }

        [Fact]
        public void Append_ThreeItem()
        {
            // Arrange
            var value1 = 7;
            var value2 = 22;
            var value3 = 3;
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(value1);
            builder.Append(value2);
            builder.Append(value3);

            // Assert
            Assert.Equal(3, builder.Count);
            Assert.Equal(new[] { value1, value2, value3 }, builder.Buffer.Take(3));
        }

        [Fact]
        public void Append_FillBuffer()
        {
            // Arrange
            var capacity = 8;
            using var builder = new ArrayBuilder<int>(minCapacity: capacity);

            // Act
            for (var i = 0; i < capacity; i++)
            {
                builder.Append(5);
            }

            // Assert
            Assert.Equal(capacity, builder.Count);
            Assert.Equal(Enumerable.Repeat(5, capacity), builder.Buffer.Take(capacity));
        }

        [Fact]
        public void AppendArray_CopySubset()
        {
            // Arrange
            var array = Enumerable.Repeat(8, 5).ToArray();
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(array, 0, 2);

            // Assert
            Assert.Equal(2, builder.Count);
            Assert.Equal(new[] { 8, 8 }, builder.Buffer.Take(2));
        }

        [Fact]
        public void AppendArray_CopyArray()
        {
            // Arrange
            var array = Enumerable.Repeat(8, 5).ToArray();
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(array, 0, array.Length);

            // Assert
            Assert.Equal(array.Length, builder.Count);
            Assert.Equal(array, builder.Buffer.Take(array.Length));
        }

        [Fact]
        public void AppendArray_AfterPriorInsertion()
        {
            // Arrange
            var array = Enumerable.Repeat(8, 5).ToArray();
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(118);
            builder.Append(array, 0, 2);

            // Assert
            Assert.Equal(3, builder.Count);
            Assert.Equal(new[] { 118, 8, 8 }, builder.Buffer.Take(3));
        }

        [Theory]
        // These are at boundaries of our capacity increments.
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1025)]
        public void AppendArray_LargerThanBuffer(int size)
        {
            // Arrange
            var array = Enumerable.Repeat(17, size).ToArray();
            using var builder = CreateArrayBuilder();

            // Act
            builder.Append(array, 0, array.Length);

            // Assert
            Assert.Equal(array.Length, builder.Count);
            Assert.Equal(array, builder.Buffer.Take(array.Length));
        }

        [Fact]
        public void Overwrite_Works()
        {
            // Arrange
            using var builder = CreateArrayBuilder();
            builder.Append(7);
            builder.Append(3);
            builder.Append(9);

            // Act
            builder.Overwrite(1, 2);

            // Assert
            Assert.Equal(3, builder.Count);
            Assert.Equal(new[]{ 7, 2, 9}, builder.Buffer.Take(3));
        }

        [Fact]
        public void Insert_Works()
        {
            // Arrange
            using var builder = CreateArrayBuilder();
            builder.Append(7);
            builder.Append(3);
            builder.Append(9);

            // Act
            builder.InsertExpensive(1, 2);

            // Assert
            Assert.Equal(4, builder.Count);
            Assert.Equal(new[] { 7, 2, 3, 9 }, builder.Buffer.Take(4));
        }

        [Fact]
        public void Insert_WhenBufferIsAtCapacity()
        {
            // Arrange
            using var builder = CreateArrayBuilder(2);
            builder.Append(new[] { 1, 3 }, 0, 2);

            // Act
            builder.InsertExpensive(1, 2);

            // Assert
            Assert.Equal(3, builder.Count);
            Assert.Equal(new[] { 1, 2, 3 }, builder.Buffer.Take(3));
        }

        [Fact]
        public void RemoveLast_Works()
        {
            // Arrange
            using var builder = CreateArrayBuilder();
            builder.Append(1);
            builder.Append(2);
            builder.Append(3);

            // Act
            builder.RemoveLast();

            // Assert
            Assert.Equal(2, builder.Count);
            Assert.Equal(new[] { 1, 2, }, builder.Buffer.Take(2));
        }

        [Fact]
        public void RemoveLast_LastEntry()
        {
            // Arrange
            int[] buffer;
            using (var builder = CreateArrayBuilder())
            {
                builder.Append(1);
                buffer = builder.Buffer;

                // Act
                builder.RemoveLast();

                // Assert
                Assert.Equal(0, builder.Count);
            }

            // Also verify that the buffer is indeed returned in this case.
            var returnedBuffer = Assert.Single(ArrayPool.ReturnedBuffers);
            Assert.Same(buffer, returnedBuffer);
        }

        [Fact]
        public void Clear_ReturnsBuffer()
        {
            // Arrange
            using var builder = CreateArrayBuilder();
            builder.Append(1);
            var buffer = builder.Buffer;

            // Act
            builder.Clear();

            // Assert
            Assert.Equal(0, builder.Count);
            var returnedBuffer = Assert.Single(ArrayPool.ReturnedBuffers);
            Assert.Same(buffer, returnedBuffer);
        }

        [Fact]
        public void Dispose_WithEmptyBuffer_DoesNotReturnIt()
        {
            // Arrange
            var builder = CreateArrayBuilder();

            // Act
            builder.Dispose();

            // Assert
            Assert.Empty(ArrayPool.ReturnedBuffers);
        }

        [Fact]
        public void Dispose_NonEmptyBufferIsReturned()
        {
            // Arrange
            var builder = CreateArrayBuilder();
            builder.Append(1);
            var buffer = builder.Buffer;

            // Act
            builder.Dispose();

            // Assert
            Assert.Single(ArrayPool.ReturnedBuffers);
            var returnedBuffer = Assert.Single(ArrayPool.ReturnedBuffers);
            Assert.Same(buffer, returnedBuffer);
            Assert.NotSame(builder.Buffer, buffer); // Prevents use after free
        }

        [Fact]
        public void DoubleDispose_DoesNotReturnBufferTwice()
        {
            // Arrange
            var builder = CreateArrayBuilder();
            builder.Append(1);
            var buffer = builder.Buffer;

            // Act
            builder.Dispose();
            builder.Dispose();

            // Assert
            Assert.Single(ArrayPool.ReturnedBuffers);
            var returnedBuffer = Assert.Single(ArrayPool.ReturnedBuffers);
            Assert.Same(buffer, returnedBuffer);
        }

        [Fact]
        public void Dispose_ThrowsOnReuse()
        {
            // Arrange
            var builder = CreateArrayBuilder();
            builder.Append(1);
            var buffer = builder.Buffer;

            builder.Dispose();
            Assert.Single(ArrayPool.ReturnedBuffers);

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => builder.Append(1));
        }

        [Fact]
        public void UnusedBufferIsReturned_OnResize()
        {
            // Arrange
            var builder = CreateArrayBuilder(2);

            // Act
            for (var i = 0; i < 10; i++)
            {
                builder.Append(i);
            }

            // Assert
            Assert.Collection(
                ArrayPool.ReturnedBuffers,
                buffer => Assert.Equal(2, buffer.Length),
                buffer => Assert.Equal(4, buffer.Length),
                buffer => Assert.Equal(8, buffer.Length));

            // Clear this because this is no longer interesting.
            ArrayPool.ReturnedBuffers.Clear();

            var buffer = builder.Buffer;
            builder.Dispose();

            Assert.Same(buffer, Assert.Single(ArrayPool.ReturnedBuffers));
        }

        private ArrayBuilder<int> CreateArrayBuilder(int capacity = 32)
        {
            return new ArrayBuilder<int>(capacity, ArrayPool);
        }
    }
}
