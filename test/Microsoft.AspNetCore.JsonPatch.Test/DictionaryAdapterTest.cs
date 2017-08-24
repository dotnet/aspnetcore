// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class DictionaryAdapterTest
    {
        [Fact]
        public void Add_KeyWhichAlreadyExists_ReplacesExistingValue()
        {
            // Arrange
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            dictionary[nameKey] = "Mike";
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);

            // Act
            var addStatus = dictionaryAdapter.TryAdd(dictionary, nameKey, resolver.Object, "James", out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Single(dictionary);
            Assert.Equal("James", dictionary[nameKey]);
        }

        [Fact]
        public void Get_UsingCaseSensitiveKey_FailureScenario()
        {
            // Arrange
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var addStatus = dictionaryAdapter.TryAdd(dictionary, nameKey, resolver.Object, "James", out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Single(dictionary);
            Assert.Equal("James", dictionary[nameKey]);

            // Act
            addStatus = dictionaryAdapter.TryGet(dictionary, nameKey.ToUpper(), resolver.Object, out var outValue, out message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Null(outValue);
        }

        [Fact]
        public void Get_UsingCaseSensitiveKey_SuccessScenario()
        {
            // Arrange
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var addStatus = dictionaryAdapter.TryAdd(dictionary, nameKey, resolver.Object, "James", out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Single(dictionary);
            Assert.Equal("James", dictionary[nameKey]);

            // Act
            addStatus = dictionaryAdapter.TryGet(dictionary, nameKey, resolver.Object, out var outValue, out message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal("James", outValue?.ToString());
        }

        [Fact]
        public void ReplacingExistingItem()
        {
            // Arrange
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            dictionary.Add(nameKey, "Mike");
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);

            // Act
            var replaceStatus = dictionaryAdapter.TryReplace(dictionary, nameKey, resolver.Object, "James", out var message);

            // Assert
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Single(dictionary);
            Assert.Equal("James", dictionary[nameKey]);
        }

        [Fact]
        public void Replace_NonExistingKey_Fails()
        {
            // Arrange
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);

            // Act
            var replaceStatus = dictionaryAdapter.TryReplace(dictionary, nameKey, resolver.Object, "Mike", out var message);

            // Assert
            Assert.False(replaceStatus);
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.", nameKey),
                message);
            Assert.Empty(dictionary);
        }

        [Fact]
        public void Remove_NonExistingKey_Fails()
        {
            // Arrange
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);

            // Act
            var removeStatus = dictionaryAdapter.TryRemove(dictionary, nameKey, resolver.Object, out var message);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.", nameKey),
                message);
            Assert.Empty(dictionary);
        }

        [Fact]
        public void Remove_RemovesFromDictionary()
        {
            // Arrange
            var nameKey = "Name";
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            dictionary[nameKey] = "James";
            var dictionaryAdapter = new DictionaryAdapter();
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);

            // Act
            var removeStatus = dictionaryAdapter.TryRemove(dictionary, nameKey, resolver.Object, out var message);

            //Assert
            Assert.True(removeStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Empty(dictionary);
        }
    }
}
