// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class MultipleAttributesDictionaryTest
    {
        [Fact]
        public void CanStoreAndRetrieveValues()
        {
            var instance = new MultipleAttributesDictionary();

            // Add key1
            Assert.True(instance.TryAdd("key1", 123, out var existingValue1));
            Assert.Equal(default, existingValue1);

            // Add key2
            Assert.True(instance.TryAdd("key2", 456, out var existingValue2));
            Assert.Equal(default, existingValue2);

            // Can't add key1 again. Instead we retrieve the existing value.
            Assert.False(instance.TryAdd("key1", 1000, out var existingValue3));
            Assert.Equal(123, existingValue3);

            // Same for KEY1, showing the keys are case-insensitive, and we didn't overwrite last time.
            Assert.False(instance.TryAdd("KEY1", 2000, out var existingValue4));
            Assert.Equal(123, existingValue4);
        }

        [Fact]
        public void HandlesKeysThatVaryOnlyByOneChar()
        {
            // Arrange
            // This test case is intended to produce clashing hashes
            var instance = new MultipleAttributesDictionary();
            instance.TryAdd("SomeLongKey", 123, out _);

            // Act
            var didAddSecondEntry = instance.TryAdd("SXmeLongKey", 456, out _);

            // Assert
            Assert.True(didAddSecondEntry);
            Assert.False(instance.TryAdd("SomeLongKey", 0, out var existingValue1));
            Assert.False(instance.TryAdd("SXmeLongKey", 0, out var existingValue2));
            Assert.Equal(123, existingValue1);
            Assert.Equal(456, existingValue2);
        }

        [Fact]
        public void CanClear()
        {
            // Arrange
            var instance = new MultipleAttributesDictionary();
            instance.TryAdd("X", 123, out _);

            // Act
            instance.Clear();

            // Assert
            Assert.True(instance.TryAdd("X", 456, out var existingValue));
            Assert.Equal(default, existingValue);
        }

        [Fact]
        public void AllowsEmptyStringKey()
        {
            // Arrange
            var instance = new MultipleAttributesDictionary();

            // Act
            instance.TryAdd(string.Empty, 1, out _);

            // Assert
            Assert.False(instance.TryAdd(string.Empty, 0, out var storedValue));
            Assert.Equal(1, storedValue);
        }

        [Fact]
        public void CanReplaceExistingValues()
        {
            // Arrange
            var instance = new MultipleAttributesDictionary();
            instance.TryAdd("somekey", 123, out _);

            // Act
            instance.Replace("SomeKey", 456);
            instance.Replace("SomeKey", 789);

            // Assert
            Assert.False(instance.TryAdd("SOMEKEY", 0, out var storedValue));
            Assert.Equal(789, storedValue);
        }

        [Fact]
        public void CannotReplaceNonExistingValues()
        {
            // Arrange
            var instance = new MultipleAttributesDictionary();
            instance.TryAdd("somekey", 123, out _);

            // Act
            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.Replace("otherkey", 456);
            });
        }

        [Fact]
        public void CanExpandStorage()
        {
            // Arrange
            var instance = new MultipleAttributesDictionary();
            int index;
            for (index = 0; index < MultipleAttributesDictionary.InitialCapacity; index++)
            {
                Assert.True(instance.TryAdd($"key{index}", index, out _));
            }

            // Act 1: Store the same amount again
            var doubledCapacity = 2 * MultipleAttributesDictionary.InitialCapacity;
            for (; index < doubledCapacity; index++)
            {
                Assert.True(instance.TryAdd($"key{index}", index, out _));
            }

            // Assert: Verify contents
            for (var i = 0; i < doubledCapacity; i++)
            {
                Assert.False(instance.TryAdd($"key{i}", 0, out var storedValue));
                Assert.Equal(i, storedValue);
            }

            // Act 2: Store a lot more
            var largeCapacity = 100 * MultipleAttributesDictionary.InitialCapacity;
            for (; index < largeCapacity; index++)
            {
                Assert.True(instance.TryAdd($"key{index}", index, out _));
            }

            // Assert: Verify contents
            for (var i = 0; i < largeCapacity; i++)
            {
                Assert.False(instance.TryAdd($"key{i}", 0, out var storedValue));
                Assert.Equal(i, storedValue);
            }
        }
    }
}
