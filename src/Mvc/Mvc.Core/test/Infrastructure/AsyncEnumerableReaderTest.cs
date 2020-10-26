// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class AsyncEnumerableReaderTest
    {
        [Theory]
        [InlineData(typeof(Range))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(List<int>))]
        public void TryGetReader_ReturnsFalse_IfTypeIsNotIAsyncEnumerable(Type type)
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable = TestEnumerable();

            // Act
            var result = readerFactory.TryGetReader(type, out var reader);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TryGetReader_ReturnsReaderForIAsyncEnumerable()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable = TestEnumerable();

            // Act
            var result = readerFactory.TryGetReader(asyncEnumerable.GetType(), out var reader);

            // Assert
            Assert.True(result);
            var readCollection = await reader(asyncEnumerable);
            var collection = Assert.IsAssignableFrom<ICollection<string>>(readCollection);
            Assert.Equal(new[] { "0", "1", "2", }, collection);
        }

        [Fact]
        public async Task TryGetReader_ReturnsReaderForIAsyncEnumerableOfValueType()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable = PrimitiveEnumerable();

            // Act
            var result = readerFactory.TryGetReader(asyncEnumerable.GetType(), out var reader);

            // Assert
            Assert.True(result);
            var readCollection = await reader(asyncEnumerable);
            var collection = Assert.IsAssignableFrom<ICollection<int>>(readCollection);
            Assert.Equal(new[] { 0, 1, 2, }, collection);
        }

        [Fact]
        public void TryGetReader_ReturnsCachedDelegate()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable1 = TestEnumerable();
            var asyncEnumerable2 = TestEnumerable();

            // Act
            Assert.True(readerFactory.TryGetReader(asyncEnumerable1.GetType(), out var reader1));
            Assert.True(readerFactory.TryGetReader(asyncEnumerable2.GetType(), out var reader2));

            // Assert
            Assert.Same(reader1, reader2);
        }

        [Fact]
        public void TryGetReader_ReturnsCachedDelegate_WhenTypeImplementsMultipleIAsyncEnumerableContracts()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable1 = new MultiAsyncEnumerable();
            var asyncEnumerable2 = new MultiAsyncEnumerable();

            // Act
            Assert.True(readerFactory.TryGetReader(asyncEnumerable1.GetType(), out var reader1));
            Assert.True(readerFactory.TryGetReader(asyncEnumerable2.GetType(), out var reader2));

            // Assert
            Assert.Same(reader1, reader2);
        }

        [Fact]
        public async Task CachedDelegate_CanReadEnumerableInstanceMultipleTimes()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable1 = TestEnumerable();
            var asyncEnumerable2 = TestEnumerable();
            var expected = new[] { "0", "1", "2" };

            // Act
            Assert.True(readerFactory.TryGetReader(asyncEnumerable1.GetType(), out var reader));

            // Assert
            Assert.Equal(expected, await reader(asyncEnumerable1));
            Assert.Equal(expected, await reader(asyncEnumerable2));
        }

        [Fact]
        public async Task CachedDelegate_CanReadEnumerableInstanceMultipleTimes_ThatProduceDifferentResults()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable1 = TestEnumerable();
            var asyncEnumerable2 = TestEnumerable(4);

            // Act
            Assert.True(readerFactory.TryGetReader(asyncEnumerable1.GetType(), out var reader));

            // Assert
            Assert.Equal(new[] { "0", "1", "2" }, await reader(asyncEnumerable1));
            Assert.Equal(new[] { "0", "1", "2", "3" }, await reader(asyncEnumerable2));
        }

        [Fact]
        public void TryGetReader_ReturnsDifferentInstancesForDifferentEnumerables()
        {
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var enumerable1 = TestEnumerable();
            var enumerable2 = TestEnumerable2();

            // Act
            Assert.True(readerFactory.TryGetReader(enumerable1.GetType(), out var reader1));
            Assert.True(readerFactory.TryGetReader(enumerable2.GetType(), out var reader2));

            // Assert
            Assert.NotSame(reader1, reader2);
        }

        [Fact]
        public async Task Reader_ReadsIAsyncEnumerable_ImplementingMultipleAsyncEnumerableInterfaces()
        {
            // This test ensures the reader does not fail if you have a type that implements IAsyncEnumerable for multiple Ts
            // Arrange
            var options = new MvcOptions();
            var readerFactory = new AsyncEnumerableReader(options);
            var asyncEnumerable = new MultiAsyncEnumerable();

            // Act
            var result = readerFactory.TryGetReader(asyncEnumerable.GetType(), out var reader);

            // Assert
            Assert.True(result);
            var readCollection = await reader(asyncEnumerable);
            var collection = Assert.IsAssignableFrom<ICollection<object>>(readCollection);
            Assert.Equal(new[] { "0", "1", "2", }, collection);
        }

        [Fact]
        public async Task Reader_ThrowsIfBufferLimitIsReached()
        {
            // Arrange
            var enumerable = TestEnumerable(11);
            var expected = $"'AsyncEnumerableReader' reached the configured maximum size of the buffer when enumerating a value of type '{enumerable.GetType()}'. " +
                "This limit is in place to prevent infinite streams of 'IAsyncEnumerable<>' from continuing indefinitely. If this is not a programming mistake, " +
                $"consider ways to reduce the collection size, or consider manually converting '{enumerable.GetType()}' into a list rather than increasing the limit.";
            var options = new MvcOptions { MaxIAsyncEnumerableBufferLimit = 10 };
            var readerFactory = new AsyncEnumerableReader(options);

            // Act
            Assert.True(readerFactory.TryGetReader(enumerable.GetType(), out var reader));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => reader(enumerable));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        public static async IAsyncEnumerable<string> TestEnumerable(int count = 3)
        {
            await Task.Yield();
            for (var i = 0; i < count; i++)
            {
                yield return i.ToString();
            }
        }

        public static async IAsyncEnumerable<string> TestEnumerable2()
        {
            await Task.Yield();
            yield return "Hello";
            yield return "world";
        }

        public static async IAsyncEnumerable<int> PrimitiveEnumerable(int count = 3)
        {
            await Task.Yield();
            for (var i = 0; i < count; i++)
            {
                yield return i;
            }
        }

        public class MultiAsyncEnumerable : IAsyncEnumerable<object>, IAsyncEnumerable<string>
        {
            public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return TestEnumerable().GetAsyncEnumerator(cancellationToken);
            }

            IAsyncEnumerator<object> IAsyncEnumerable<object>.GetAsyncEnumerator(CancellationToken cancellationToken)
                => GetAsyncEnumerator(cancellationToken);
        }
    }
}
