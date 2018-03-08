// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class ConcurrentLruCacheTest
    {
        [Fact]
        public void ConcurrentLruCache_HoldsCapacity()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var expected = input.Reverse();

            // Act
            var cache = new ConcurrentLruCache<int, int>(input);

            // Assert
            Assert.Equal(expected, cache.TestingEnumerable);
        }

        [Fact]
        public void Add_ThrowsIfKeyExists()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => cache.Add(1, 1));
            Assert.StartsWith("Key already exists", exception.Message);
        }

        [Fact]
        public void GetOrAdd_AddsIfKeyDoesNotExist()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var expected = GetKeyValueArray(Enumerable.Range(2, 3)).Reverse();
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act
            cache.GetOrAdd(4, 4);

            // Assert
            Assert.Equal(expected, cache.TestingEnumerable);
        }

        [Fact]
        public void Remove_RemovesEntry()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var expected = GetKeyValueArray(Enumerable.Range(1, 2)).Reverse();
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act
            var result = cache.Remove(3);

            // Assert
            Assert.True(result);
            Assert.Equal(expected, cache.TestingEnumerable);
        }

        [Fact]
        public void Remove_KeyNotFound_ReturnsFalse()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act
            var result = cache.Remove(4);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Add_NoRead_EvictsLastNode()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var expected = GetKeyValueArray(Enumerable.Range(2, 3)).Reverse();
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act
            cache.Add(4, 4);

            // Assert
            Assert.Equal(expected, cache.TestingEnumerable);
        }

        [Fact]
        public void Add_ReadLastNode_EvictsSecondOldestNode()
        {
            // Arrange
            var input = GetKeyValueArray(Enumerable.Range(1, 3));
            var expected = GetKeyValueArray(new int[] { 4, 1, 3 });
            var cache = new ConcurrentLruCache<int, int>(input);

            // Act
            cache.GetOrAdd(1, 1); // Read to make this MRU
            cache.Add(4, 4); // Add a new node

            // Assert
            Assert.Equal(expected, cache.TestingEnumerable);
        }

        private KeyValuePair<int, int>[] GetKeyValueArray(IEnumerable<int> inputArray)
        {
            return inputArray.Select(v => new KeyValuePair<int, int>(v, v)).ToArray();
        }
    }
}
