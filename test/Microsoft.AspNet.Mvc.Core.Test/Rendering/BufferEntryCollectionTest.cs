// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class BufferEntryCollectionTest
    {
        [Fact]
        public void Add_AddsBufferEntries()
        {
            // Arrange
            var collection = new BufferEntryCollection();
            var inner = new BufferEntryCollection();

            // Act
            collection.Add("Hello");
            collection.Add(new[] { 'a', 'b', 'c' }, 1, 2);
            collection.Add(inner);

            // Assert
            Assert.Equal("Hello", collection.BufferEntries[0]);
            Assert.Equal("bc", collection.BufferEntries[1]);
            Assert.Same(inner.BufferEntries, collection.BufferEntries[2]);
        }

        [Fact]
        public void AddChar_ThrowsIfIndexIsOutOfBounds()
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act and Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.Add(new[] { 'a', 'b', 'c' }, -1, 2));
            Assert.Equal("index", ex.ParamName);
        }

        [Fact]
        public void AddChar_ThrowsIfCountWouldCauseOutOfBoundReads()
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act and Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.Add(new[] { 'a', 'b', 'c' }, 1, 3));
            Assert.Equal("count", ex.ParamName);
        }

        public static IEnumerable<object[]> AddWithChar_RepresentsStringsAsChunkedEntriesData
        {
            get
            {
                var charArray1 = new[] { 'a' };
                var expected1 = new[] { "a" };
                yield return new object[] { charArray1, 0, 1, expected1 };

                var charArray2 = Enumerable.Repeat('a', 10).ToArray();
                var expected2 = new[] { new string(charArray2) };
                yield return new object[] { charArray2, 0, 10, expected2 };

                var charArray3 = Enumerable.Repeat('b', 1024).ToArray();
                var expected3 = new[] { new string('b', 1023) };
                yield return new object[] { charArray3, 1, 1023, expected3 };

                var charArray4 = Enumerable.Repeat('c', 1027).ToArray();
                var expected4 = new[] { new string('c', 1024), "cc" };
                yield return new object[] { charArray4, 1, 1026, expected4 };

                var charArray5 = Enumerable.Repeat('d', 4099).ToArray();
                var expected5 = new[] { new string('d', 1024), new string('d', 1024), new string('d', 1024), new string('d', 1024), "d" };
                yield return new object[] { charArray5, 2, 4097, expected5 };

                var charArray6 = Enumerable.Repeat('e', 1025).ToArray();
                var expected6 = new[] { "ee" };
                yield return new object[] { charArray6, 1023, 2, expected6 };
            }
        }

        [Theory]
        [MemberData(nameof(AddWithChar_RepresentsStringsAsChunkedEntriesData))]
        public void AddWithChar_RepresentsStringsAsChunkedEntries(char[] value, int index, int count, IList<object> expected)
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act
            collection.Add(value, index, count);

            // Assert
            Assert.Equal(expected, collection.BufferEntries);
        }

        public static IEnumerable<object[]> Enumerator_TraversesThroughBufferData
        {
            get
            {
                var collection1 = new BufferEntryCollection();
                collection1.Add("foo");
                collection1.Add("bar");

                var expected1 = new[]
                {
                    "foo",
                    "bar"
                };
                yield return new object[] { collection1, expected1 };

                // Nested collection
                var nestedCollection2 = new BufferEntryCollection();
                nestedCollection2.Add("level 1");
                var nestedCollection2SecondLevel = new BufferEntryCollection();
                nestedCollection2SecondLevel.Add("level 2");
                nestedCollection2.Add(nestedCollection2SecondLevel);
                var collection2 = new BufferEntryCollection();
                collection2.Add("foo");
                collection2.Add(nestedCollection2);
                collection2.Add("qux");

                var expected2 = new[]
                {
                    "foo",
                    "level 1",
                    "level 2",
                    "qux"
                };
                yield return new object[] { collection2, expected2 };

                // Nested collection
                var collection3 = new BufferEntryCollection();
                collection3.Add("Hello");
                var emptyNestedCollection = new BufferEntryCollection();
                emptyNestedCollection.Add(new BufferEntryCollection());
                collection3.Add(emptyNestedCollection);
                collection3.Add("world");

                var expected3 = new[]
                {
                    "Hello",
                    "world"
                };
                yield return new object[] { collection3, expected3 };
            }
        }


        [Theory]
        [MemberData(nameof(Enumerator_TraversesThroughBufferData))]
        public void Enumerator_TraversesThroughBuffer(BufferEntryCollection buffer, string[] expected)
        {
            // Act and Assert
            Assert.Equal(expected, buffer);
        }
    }
}