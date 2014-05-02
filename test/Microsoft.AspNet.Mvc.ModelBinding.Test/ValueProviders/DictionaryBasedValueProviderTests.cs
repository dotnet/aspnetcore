// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProviderTestss
    {
        [Fact]
        public async Task GetValueProvider_ReturnsNull_WhenKeyIsNotFound()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "value" }
            };
            var provider = new DictionaryBasedValueProvider(values);
            
            // Act
            var result = await provider.GetValueAsync("not-test-key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetValueProvider_ReturnsValue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.GetValueAsync("test-key");

            // Assert
            Assert.Equal("test-value", result.RawValue);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsNullValue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", null }
            };
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.GetValueAsync("test-key");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RawValue);
            Assert.Null(result.AttemptedValue);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsFalse_IfKeyIsNotPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.ContainsPrefixAsync("not-test-key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ContainsPrefixAsync_ReturnsTrue_IfKeyIsPresent()
        {
            // Arrange
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "test-key", "test-value" }
            };
            var provider = new DictionaryBasedValueProvider(values);

            // Act
            var result = await provider.ContainsPrefixAsync("test-key");

            // Assert
            Assert.True(result);
        }
    }
}
