// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class AsyncEnumerableReaderTest
    {
        [Fact]
        public async Task ReadAsync_ReadsIAsyncEnumerable()
        {
            // Arrange
            var options = new MvcOptions();
            var reader = new AsyncEnumerableReader(options);

            // Act
            var result = await reader.ReadAsync(TestEnumerable());

            // Assert
            var collection = Assert.IsAssignableFrom<ICollection<string>>(result);
            Assert.Equal(new[] { "0", "1", "2", }, collection);
        }

        [Fact]
        public async Task ReadAsync_ReadsIAsyncEnumerable_ImplementingMultipleAsyncEnumerableInterfaces()
        {
            // This test ensures the reader does not fail if you have a type that implements IAsyncEnumerable for multiple Ts
            // Arrange
            var options = new MvcOptions();
            var reader = new AsyncEnumerableReader(options);

            // Act
            var result = await reader.ReadAsync(new MultiAsyncEnumerable());

            // Assert
            var collection = Assert.IsAssignableFrom<ICollection<object>>(result);
            Assert.Equal(new[] { "0", "1", "2", }, collection);
        }

       [Fact]
        public async Task ReadAsync_ThrowsIfBufferimitIsReached()
        {
            // Arrange
            var enumerable = TestEnumerable(11);
            var expected = $"'AsyncEnumerableReader' reached the configured maximum size of the buffer when enumerating a value of type '{enumerable.GetType()}'. " +
                "This limit is in place to prevent infinite streams of 'IAsyncEnumerable<>' from continuing indefinitely. If this is not a programming mistake, " +
                $"consider ways to reduce the collection size, or consider manually converting '{enumerable.GetType()}' into a list rather than increasing the limit.";
            var options = new MvcOptions { MaxIAsyncEnumerableBufferLimit = 10 };
            var reader = new AsyncEnumerableReader(options);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync(enumerable));

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
